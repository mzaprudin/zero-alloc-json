#!/usr/bin/env python3
"""Select documentation checks or the complete runtime matrix from the event diff."""
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
PRESENTATION_SCRIPTS = frozenset(('scripts/render-results.py', 'scripts/verify-evidence.py'))
IMAGE_SUFFIXES = frozenset(('.svg', '.png', '.webp', '.gif', '.jpg', '.jpeg'))
SHA_PATTERN = re.compile(r'(?:[0-9a-f]{40}|[0-9a-f]{64})')


def needs_full_checks(paths):
    """Allow known documentation inputs; runtime, CI and unknown files require full CI."""
    for name in paths:
        path = PurePosixPath(name)
        if ((path.parent == PurePosixPath('.') and path.suffix == '.md')
                or name.startswith(('docs/', 'results/'))
                or (name.startswith('assets/readme/') and path.suffix.lower() in IMAGE_SUFFIXES)
                or name in PRESENTATION_SCRIPTS):
            continue
        return True
    return False


def changed_paths(event_name, event, root=ROOT):
    """Use the whole push or PR range, retaining both paths of renamed files."""
    if not isinstance(event, dict):
        return None
    try:
        if event_name == 'push':
            base, head = event['before'], event['after']
        elif event_name == 'pull_request':
            request = event['pull_request']
            base, head = request['base']['sha'], request['head']['sha']
        else:
            return None
    except (KeyError, TypeError):
        return None
    if any(not isinstance(sha, str) or not SHA_PATTERN.fullmatch(sha) or set(sha) == {'0'}
           for sha in (base, head)):
        return None
    try:
        if event_name == 'pull_request':
            base = subprocess.check_output(
                ['git', 'merge-base', base, head], cwd=root, stderr=subprocess.PIPE,
                text=True).strip()
        output = subprocess.check_output(
            ['git', 'diff', '--no-renames', '--name-only', '-z', base, head, '--'],
            cwd=root, stderr=subprocess.PIPE)
    except (subprocess.CalledProcessError, OSError):
        return None
    return [name.decode('utf-8', errors='surrogateescape')
            for name in output.split(b'\0') if name]


def main():
    """Missing history or event data and manual runs select the full matrix."""
    try:
        event = json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text(encoding='utf-8'))
    except (KeyError, OSError, ValueError):
        event = None
    paths = changed_paths(os.environ.get('GITHUB_EVENT_NAME'), event)
    full = paths is None or needs_full_checks(paths)
    print(json.dumps({'full': full, 'paths': paths, 'fallback': paths is None}, indent=2))
    with Path(os.environ['GITHUB_OUTPUT']).open('a', encoding='utf-8') as output:
        output.write(f'full={str(full).lower()}\n')


if __name__ == '__main__':
    main()
