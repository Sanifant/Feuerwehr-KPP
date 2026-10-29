import tempfile
from pathlib import Path
import unittest

from summarize_tests import parse_report, summarize


TRX = """<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
<Results>
  <UnitTestResult testName="Backend.Pass" outcome="Passed" duration="00:00:01.25" />
  <UnitTestResult testName="Backend.Fail" outcome="Failed" duration="00:01:00">
    <Output><ErrorInfo><Message>Expected &lt;value&gt;</Message></ErrorInfo></Output>
  </UnitTestResult>
  <UnitTestResult testName="Backend.Skip" outcome="NotExecuted" />
</Results></TestRun>"""

JUNIT = """<testsuites tests="99"><testsuite tests="99">
  <testcase classname="page.test.tsx" name="passes" time="0.5" />
  <testcase classname="page.test.tsx" name="assertion failure"><failure message="assertion">stack</failure></testcase>
  <testcase classname="page.test.tsx" name="runner error"><error message="error">stack</error></testcase>
  <testcase classname="page.test.tsx" name="skipped"><skipped /></testcase>
</testsuite></testsuites>"""


class TestSummaryTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def report(self, name, content):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        return path

    def test_trx_namespace_outcomes_duration_and_failure(self):
        cases = parse_report(self.report("backend-tests.trx", TRX))
        self.assertEqual([case.outcome for case in cases], ["Passed", "Failed", "Skipped"])
        self.assertEqual([case.seconds for case in cases], [1.25, 60, 0])
        self.assertEqual(cases[1].message, "Expected <value>")

    def test_junit_nested_suites_failures_errors_and_skips(self):
        cases = parse_report(self.report("vitest-junit.xml", JUNIT))
        self.assertEqual(len(cases), 4)  # Ignore aggregate attributes; don't double count.
        self.assertEqual([case.outcome for case in cases], ["Passed", "Failed", "Failed", "Skipped"])
        self.assertEqual(cases[0].name, "page.test.tsx passes")
        self.assertEqual(cases[0].seconds, 0.5)
        self.assertIn("assertion", cases[1].message)
        self.assertIn("error", cases[2].message)

    def test_unknown_trx_outcome_is_not_reported_as_passed(self):
        cases = parse_report(self.report("backend-tests.trx", TRX.replace('outcome="Passed"', 'outcome="Error"')))
        self.assertEqual(cases[0].outcome, "Failed")

    def test_all_suites_are_aggregated_across_artifact_directories(self):
        for name in ("backend-tests.trx", "app-tests.trx", "integration-tests.trx"):
            self.report(f"dotnet-artifact/{name}", TRX)
        self.report("frontend-artifact/TestResults/vitest-junit.xml", JUNIT)
        summary, invalid = summarize(self.root, {})
        self.assertFalse(invalid)
        self.assertIn("**13** | **4** | **5** | **4** | **184.25s**", summary)
        for label in ("Backend unit tests", "App unit tests", "Frontend tests", "API integration tests"):
            self.assertIn(f"<summary>{label}: all", summary)
        self.assertIn("Expected &lt;value&gt;", summary)

    def test_missing_reports_show_job_status_without_fake_zero_results(self):
        summary, invalid = summarize(self.root, {
            "backend-build-and-test": {"result": "failure"},
            "frontend-build-and-test": {"result": "cancelled"},
            "integration-tests": {"result": "skipped"},
        })
        self.assertFalse(invalid)
        self.assertIn("| Backend unit tests | failure | — |", summary)
        self.assertIn("| Frontend tests | cancelled | — |", summary)
        self.assertIn("| API integration tests | skipped | — |", summary)
        self.assertIn("No report available", summary)

    def test_successful_job_without_report_is_a_reporting_error(self):
        _, invalid = summarize(self.root, {"frontend-build-and-test": {"result": "success"}})
        self.assertTrue(invalid)

    def test_malformed_and_empty_reports_do_not_hide_other_results(self):
        self.report("backend-tests.trx", "<broken")
        self.report("app-tests.trx", "<TestRun />")
        self.report("vitest-junit.xml", JUNIT)
        summary, invalid = summarize(self.root, {})
        self.assertTrue(invalid)
        self.assertIn("Could not read backend-tests.trx", summary)
        self.assertIn("Report contains no test results", summary)
        self.assertIn("| Frontend tests | unknown | 4 | 1 | 2 | 1 |", summary)

    def test_test_names_and_failure_messages_are_escaped(self):
        self.report("backend-tests.trx", TRX.replace("Backend.Fail", "&lt;script&gt;|`*_"))
        summary, _ = summarize(self.root, {})
        self.assertNotIn("<script>", summary)
        self.assertIn("&lt;script&gt;&#124;&#96;&#42;&#95;", summary)

    def test_large_inventory_fits_the_github_summary_limit(self):
        cases = '<UnitTestResult testName="' + "long-name" * 250 + '" outcome="Passed" />'
        self.report("backend-tests.trx", "<TestRun><Results>" + cases * 1000 + "</Results></TestRun>")
        summary, invalid = summarize(self.root, {})
        self.assertFalse(invalid)
        self.assertIn("| Backend unit tests | unknown | 1000 | 1000 | 0 | 0 |", summary)
        self.assertLess(len(summary.encode("utf-8")), 1024 * 1024)
        self.assertIn("Some test details were omitted", summary)
        self.assertEqual(summary.count("<details>"), summary.count("</details>"))


if __name__ == "__main__":
    unittest.main()
