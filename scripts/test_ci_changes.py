"""Exercise CI decisions using actual multi-commit push and pull-request histories."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from ci_changes import ROOT, changed_paths, needs_full_checks


class ScopeTests(unittest.TestCase):
    def test_known_documentation_and_assets(self):
        for name in ('README.md', 'README.ru.md', 'AGENTS.md', 'docs/architecture.md',
                     'docs/upstream/source-manifest.txt', 'results/local/run/report.json',
                     'assets/readme/hero-en.svg', 'assets/readme/demo.webp',
                     'scripts/render-results.py', 'scripts/verify-evidence.py'):
            with self.subTest(path=name):
                self.assertFalse(needs_full_checks([name]))

    def test_runtime_ci_and_unknown_inputs_keep_full_checks(self):
        for name in ('src/ZeroAlloc.Json/Parser.cs', 'tests/ParserTests.cs', 'support/Trade.cs',
                     'samples/Demo.cs', 'benchmarks/Load.cs', 'fixtures/trade.json',
                     'global.json', 'Directory.Build.props', 'ZeroAlloc.Json.sln',
                     'src/packages.lock.json', '.github/workflows/ci.yml',
                     'scripts/ci_changes.py', 'scripts/test_ci_changes.py',
                     'scripts/verify-docs.py', 'scripts/test_verify_docs.py',
                     'scripts/run-benchmarks.py', 'assets/readme/script.js',
                     'unknown-input.dat', 'NOTICE'):
            with self.subTest(path=name):
                self.assertTrue(needs_full_checks(['README.md', name]))

    def test_empty_push_diff_needs_only_documentation_checks(self):
        self.assertFalse(needs_full_checks([]))

    def test_manual_new_branch_and_incomplete_events_fall_back(self):
        events = [('workflow_dispatch', {}), ('push', {'before': '0' * 40, 'after': 'a' * 40}),
                  ('push', {}), ('pull_request', {'pull_request': None}), ('push', None),
                  ('push', {'before': 123, 'after': 'a' * 40})]
        for event_name, event in events:
            with self.subTest(event_name=event_name, event=event):
                self.assertIsNone(changed_paths(event_name, event))

    def test_manual_command_emits_full_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            event, output = root / 'event.json', root / 'output.txt'
            event.write_text('{}', encoding='utf-8')
            env = dict(os.environ, GITHUB_EVENT_NAME='workflow_dispatch',
                       GITHUB_EVENT_PATH=str(event), GITHUB_OUTPUT=str(output))
            result = subprocess.check_output([sys.executable, str(ROOT / 'scripts/ci_changes.py')],
                                             env=env, text=True)
            self.assertTrue(json.loads(result)['full'])
            self.assertEqual('full=true\n', output.read_text(encoding='utf-8'))


class GitDiffTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.git('init', '-q')
        self.git('config', 'user.name', 'CI routing test')
        self.git('config', 'user.email', 'ci@example.invalid')
        (self.root / 'src').mkdir()
        (self.root / 'src/Parser.cs').write_text('original\n', encoding='utf-8')
        self.base = self.commit()

    def git(self, *arguments):
        return subprocess.check_output(['git', *arguments], cwd=self.root,
                                       stderr=subprocess.STDOUT, text=True).strip()

    def commit(self):
        self.git('add', '.')
        self.git('commit', '-qm', 'test fixture')
        return self.git('rev-parse', 'HEAD')

    def push_paths(self, head):
        return changed_paths('push', {'before': self.base, 'after': head}, self.root)

    def test_earlier_runtime_change_in_push_is_not_hidden_by_later_readme(self):
        (self.root / 'src/Parser.cs').write_text('changed\n', encoding='utf-8')
        self.commit()
        (self.root / 'README.md').write_text('docs\n', encoding='utf-8')
        paths = self.push_paths(self.commit())
        self.assertEqual({'src/Parser.cs', 'README.md'}, set(paths))
        self.assertTrue(needs_full_checks(paths))

    def test_runtime_rename_into_docs_retains_deleted_code_path(self):
        (self.root / 'docs').mkdir()
        self.git('mv', 'src/Parser.cs', 'docs/Parser.md')
        paths = self.push_paths(self.commit())
        self.assertEqual({'src/Parser.cs', 'docs/Parser.md'}, set(paths))
        self.assertTrue(needs_full_checks(paths))

    def test_runtime_deletion_requires_full_checks(self):
        self.git('rm', 'src/Parser.cs')
        self.assertTrue(needs_full_checks(self.push_paths(self.commit())))

    def test_doc_only_range_preserves_utf8_names_and_spaces(self):
        (self.root / 'docs').mkdir()
        (self.root / 'docs/Пример данных.md').write_text('docs\n', encoding='utf-8')
        self.commit()
        (self.root / 'README.md').write_text('docs\n', encoding='utf-8')
        paths = self.push_paths(self.commit())
        self.assertEqual({'docs/Пример данных.md', 'README.md'}, set(paths))
        self.assertFalse(needs_full_checks(paths))

    def test_pr_uses_merge_base_instead_of_base_tip(self):
        self.git('checkout', '-qb', 'feature')
        (self.root / 'README.md').write_text('docs\n', encoding='utf-8')
        head = self.commit()
        self.git('checkout', '-q', '--detach', self.base)
        (self.root / 'src/Parser.cs').write_text('base branch change\n', encoding='utf-8')
        base_tip = self.commit()
        event = {'pull_request': {'base': {'sha': base_tip}, 'head': {'sha': head}}}
        self.assertEqual(['README.md'], changed_paths('pull_request', event, self.root))

    def test_unavailable_history_falls_back_to_full(self):
        self.assertIsNone(changed_paths('push', {'before': 'f' * 40, 'after': self.base}, self.root))


if __name__ == '__main__':
    unittest.main()
