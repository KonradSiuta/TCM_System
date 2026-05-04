namespace TCM.Shared.Models
{
    public enum TestCaseParserType { None, DoxygenStyle, PythonDocstring }
    public enum TestResultParserType { None, EcuSimulatorXml, JUnitXml }
    public enum TestCaseSource { Manual, Imported_Header }
    public enum TestType { Unit, Integration, System, E2E }
    public enum ExecutionStatus { Unexecuted, Passed, Failed, Blocked }
    public enum ExecutionMode { Manual, Automated }
}