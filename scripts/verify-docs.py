#!/usr/bin/env python3
"""Check active documentation, README assets and measured evidence without .NET."""
from html.parser import HTMLParser
import math
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
LINK_PATTERN = re.compile(r'!?\[[^\]]*\]\((<[^>]+>|[^)\s]+)(?:\s+"[^"]*")?\)')
FENCE_PATTERN = re.compile(r'^ {0,3}(`{3,}|~{3,})(.*)$')
SVG_NAMESPACE = 'http://www.w3.org/2000/svg'


class HtmlLinks(HTMLParser):
    """Collect HTML links and image paths that Markdown-only checks miss."""
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.targets = []

    def handle_starttag(self, tag, attrs):
        attribute = {'a': 'href', 'img': 'src', 'source': 'src'}.get(tag)
        for name, value in attrs:
            if name == attribute and value:
                self.targets.append(value)


def visible_markup(content):
    """Ignore fenced examples when checking links and HTML image elements."""
    fence = None
    lines = []
    for line in content.splitlines():
        match = FENCE_PATTERN.match(line)
        if fence:
            if match and match[1][0] == fence[0] and len(match[1]) >= len(fence) and not match[2].strip():
                fence = None
            continue
        if match:
            fence = match[1]
        else:
            lines.append(line)
    return '\n'.join(lines)


def validate_document(path, root=ROOT):
    """Check formatting and existing local targets, including HTML img src."""
    content = path.read_text(encoding='utf-8')
    if not content.endswith('\n'):
        raise ValueError(f'{path}: missing final newline')
    for number, line in enumerate(content.splitlines(), 1):
        if line.endswith('\t') or (line.endswith(' ') and not line.endswith('  ')):
            raise ValueError(f'{path}:{number}: trailing whitespace')
    markup = visible_markup(content)
    parser = HtmlLinks()
    parser.feed(markup)
    targets = [target.strip('<>') for target in LINK_PATTERN.findall(markup)] + parser.targets
    for target in targets:
        parsed = urlsplit(target)
        if parsed.scheme == 'file':
            raise ValueError(f'{path}: local file URI {target}')
        if parsed.scheme or parsed.netloc or not parsed.path:
            continue
        destination = (path.parent / unquote(parsed.path)).resolve()
        if not destination.is_relative_to(root.resolve()) or not destination.exists():
            raise ValueError(f'{path}: missing or out-of-repository local link {target}')


def validate_svg(path):
    """Reject malformed SVGs or dependencies unavailable to GitHub image embeds."""
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as error:
        raise ValueError(f'{path}: malformed SVG: {error}') from error
    if root.tag != f'{{{SVG_NAMESPACE}}}svg':
        raise ValueError(f'{path}: missing SVG namespace/root')
    try:
        viewbox = [float(value) for value in root.attrib.get('viewBox', '').split()]
        valid_viewbox = (len(viewbox) == 4 and all(math.isfinite(value) for value in viewbox)
                         and viewbox[2] > 0 and viewbox[3] > 0)
    except ValueError:
        valid_viewbox = False
    if not valid_viewbox:
        raise ValueError(f'{path}: invalid or missing viewBox')
    ids = {node.attrib['id'] for node in root.iter() if 'id' in node.attrib}
    for node in root.iter():
        tag = node.tag.rsplit('}', 1)[-1]
        if tag in ('script', 'foreignObject'):
            raise ValueError(f'{path}: unsupported SVG element {tag}')
        values = [*node.attrib.values(), node.text or '']
        for name, value in node.attrib.items():
            if name.rsplit('}', 1)[-1] == 'href' and (not value.startswith('#') or value[1:] not in ids):
                raise ValueError(f'{path}: unresolved or external SVG reference {value}')
        for value in values:
            if re.search(r'@import\b', value, re.IGNORECASE):
                raise ValueError(f'{path}: external SVG stylesheet')
            for reference in re.findall(r'url\(\s*[\'\"]?([^\s\)\'\"]+)', value, re.IGNORECASE):
                if not reference.startswith('#') or reference[1:] not in ids:
                    raise ValueError(f'{path}: unresolved or external SVG dependency {reference}')


def validate_readme_svgs(root=ROOT):
    """Check every SVG selected by the classifier, regardless of extension case."""
    assets = sorted(path for path in (root / 'assets/readme').rglob('*')
                    if path.is_file() and path.suffix.lower() == '.svg')
    for path in assets:
        validate_svg(path)
    return len(assets)


def main():
    """Validate active pages/assets; leave historical bundles byte-identical."""
    for path in sorted((ROOT / 'scripts').glob('*.py')):
        compile(path.read_text(encoding='utf-8'), str(path), 'exec')
    documents = (sorted(ROOT.glob('*.md')) + sorted((ROOT / 'docs').rglob('*.md'))
                 + sorted((ROOT / 'fixtures').glob('README*.md'))
                 + [ROOT / 'results/README.md', ROOT / 'results/local/README.md'])
    for path in documents:
        validate_document(path)
    asset_count = validate_readme_svgs()
    subprocess.run([sys.executable, str(ROOT / 'scripts/verify-evidence.py')], cwd=ROOT, check=True)
    print(f'PASS: {len(documents)} Markdown files, HTML image paths, {asset_count} SVGs, Python syntax and measured evidence.')


if __name__ == '__main__':
    main()
