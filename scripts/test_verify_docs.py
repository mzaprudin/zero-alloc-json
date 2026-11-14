"""Check failures that would leave README links or SVG image embeds broken."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('verify_docs', Path(__file__).with_name('verify-docs.py'))
verify_docs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verify_docs)
VALID_SVG = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 40"><rect width="100" height="40"/></svg>'


class DocumentationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.readme = self.root / 'README.md'
        self.svg = self.root / 'hero.svg'
        self.svg.write_text(VALID_SVG, encoding='utf-8')

    def validate(self, content):
        self.readme.write_text(content, encoding='utf-8')
        verify_docs.validate_document(self.readme, self.root)

    def test_markdown_and_html_links_with_spaces_and_fragments(self):
        (self.root / 'Русский пример.md').write_text('example\n', encoding='utf-8')
        self.validate('[Русский](<Русский пример.md>)\n<img src="hero.svg"/>\n'
                      '<a href="Русский%20пример.md#section">Example</a>\n')

    def test_missing_html_image_and_link_fail(self):
        for content in ('<img src="missing.svg">\n', '<a href="missing.md">Example</a>\n'):
            with self.subTest(content=content), self.assertRaisesRegex(ValueError, 'missing'):
                self.validate(content)

    def test_missing_markdown_link_fails(self):
        with self.assertRaisesRegex(ValueError, 'missing'):
            self.validate('[Example](missing.md)\n')

    def test_fenced_examples_and_external_links_are_not_local_dependencies(self):
        self.validate('```html\n<img src="placeholder.svg">\n```\n'
                      '~~~markdown\n[Example](placeholder.md)\n~~~\n'
                      '[External](https://example.invalid/) [Section](#section)\n')

    def test_private_or_outside_repository_paths_fail(self):
        for target in ('file:///private/example.md', '../outside.md'):
            with self.subTest(target=target), self.assertRaises(ValueError):
                self.validate(f'[Example]({target})\n')

    def test_formatting_preserves_markdown_hard_breaks(self):
        self.validate('Intentional hard break  \nNext line\n')
        for content in ('Trailing space \n', 'Trailing tab\t\n', 'No final newline'):
            with self.subTest(content=content), self.assertRaises(ValueError):
                self.validate(content)

    def test_valid_svg_with_internal_clip_reference(self):
        self.svg.write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 40">'
                            '<defs><clipPath id="clip"><rect width="100" height="40"/></clipPath></defs>'
                            '<g clip-path="url(#clip)"><text>JSON</text></g></svg>', encoding='utf-8')
        verify_docs.validate_svg(self.svg)

    def test_malformed_svg_fails(self):
        self.svg.write_text('<svg>', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'malformed SVG'):
            verify_docs.validate_svg(self.svg)

    def test_uppercase_readme_svg_is_not_skipped(self):
        assets = self.root / 'assets/readme'
        assets.mkdir(parents=True)
        (assets / 'hero.SVG').write_text('<svg>', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, 'malformed SVG'):
            verify_docs.validate_readme_svgs(self.root)

    def test_svg_requires_positive_viewbox_dimensions(self):
        for viewbox in ('', '0 0 0 40', '0 0 100 -40', '0 0 nan 40', '0 0 inf 40', 'not a viewbox'):
            with self.subTest(viewbox=viewbox), self.assertRaisesRegex(ValueError, 'viewBox'):
                self.svg.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{viewbox}"/>', encoding='utf-8')
                verify_docs.validate_svg(self.svg)

    def test_unsupported_or_unresolved_svg_dependencies_fail(self):
        for element in ('<image href="https://example.invalid/image.png"/>',
                        '<image href="local.png"/>', '<g clip-path="url(#missing)"/>',
                        '<style>@import "https://example.invalid/style.css";</style>',
                        '<rect fill="url(https://example.invalid/paint.svg)"/>',
                        '<script/>', '<foreignObject/>'):
            with self.subTest(element=element), self.assertRaises(ValueError):
                self.svg.write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 40">'
                                    + element + '</svg>', encoding='utf-8')
                verify_docs.validate_svg(self.svg)


if __name__ == '__main__':
    unittest.main()
