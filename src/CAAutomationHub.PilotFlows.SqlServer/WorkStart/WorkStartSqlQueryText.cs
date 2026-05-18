namespace CAAutomationHub.PilotFlows.SqlServer.WorkStart;

public static class WorkStartSqlQueryText
{
    public const string Default = """
        SELECT PROFILE, TBLR, WIN_TYPE, CUT_SIZE, LR, RollerYN, ROLLER_HOLE_POS, ROLLER_HOLE_WIDTH, ROLLER_HOLE_LENGTH, ROLLER_TYPE, CUT_DEGREE
        FROM (
            SELECT WDL.Profile PROFILE
                , WDL.LR TBLR
                , WDL.WIN_TYPE
                , WDL.CutSize CUT_SIZE
                , '' LR
                , CASE WHEN WDL.Roller = 'S' OR WDL.Roller = 'T' THEN 'Y' ELSE 'N' END RollerYN
                , CASE WHEN WDL.Roller = 'S' THEN PS.RollerHoleOffsetSingle * 100
                       WHEN WDL.Roller = 'T' THEN PS.RollerHoleOffsetTwin * 100
                       ELSE 0
                  END ROLLER_HOLE_POS
                , ISNULL(R.roller_width, 0) * 100 ROLLER_HOLE_WIDTH
                , ISNULL(R.roller_length, 0) * 100 ROLLER_HOLE_LENGTH
                , WDL.Roller ROLLER_TYPE
                , WDL.CutDegreeLeft CUT_DEGREE
            FROM WorkDataList WDL
                 LEFT OUTER JOIN vwProfileSpecInfoBf PS
                   ON PS.ProfileName = WDL.profile AND PS.ClientNo = WDL.ClientNo
                 LEFT OUTER JOIN vwSubsidiaryRoller R
                   ON (R.roller_name = PS.RollerNameSingle OR R.roller_name = PS.RollerNameTwin)
                  AND R.roller_kind = WDL.RollerType
                  AND R.ClientNo = WDL.ClientNo
            WHERE WDL.LotId = @LotId
        ) A
        """;
}
