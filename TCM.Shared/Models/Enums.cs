namespace TCM.Shared.Models
{
    public enum TestCaseParserType { None, DoxygenStyle, PythonDocstring }
    public enum TestResultParserType { None, EcuSimulatorXml, JUnitXml }
    public enum TestCaseSource { Manual, Imported_Header }
    public enum TestType { Unit, Integration, System, E2E }
    public enum ExecutionStatus { Unexecuted, Passed, Failed, Blocked }
    public enum ExecutionMode { Manual, Automated }
    public enum ProjectStatus { Active, Finished }
    public enum RequirementStatus { Created, InReview, Accepted, Canceled }
    public enum ProjectRole { Administrator, Tester, Viewer }
}