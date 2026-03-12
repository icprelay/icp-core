-- Insert EventTypes
;WITH v AS (
    SELECT *
    FROM (VALUES
        ('core-smoke','Event','{}','Smoke test','heart-pulse'),
        ('count-blob','Schedule,OnDemand','{}','Count blobs (smoke test)','heart-pulse')
    ) AS x([Name], [AllowedTriggerTypes], [ParametersTemplateJson], [DisplayName], [IconKey])
)
MERGE dbo.EventTypes WITH (HOLDLOCK) AS t
USING v AS s
    ON t.[Name] = s.[Name]
WHEN MATCHED THEN
    UPDATE SET
        t.[AllowedTriggerTypes]      = s.[AllowedTriggerTypes],
        t.[ParametersTemplateJson]   = s.[ParametersTemplateJson],
        t.[DisplayName]              = s.[DisplayName],
        t.[IconKey]                  = s.[IconKey]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Name], [AllowedTriggerTypes], [ParametersTemplateJson], [DisplayName], [IconKey])
    VALUES (s.[Name], s.[AllowedTriggerTypes], s.[ParametersTemplateJson], s.[DisplayName], s.[IconKey]);
-- No delete branch on purpose (avoid accidental removals) 

--Seed IntegrationTargets:

-- 1) UPDATE existing rows (idempotent)
UPDATE t
SET
    t.ParametersTemplateJson = v.ParametersTemplateJson,
    t.SecretsTemplateJson    = v.SecretsTemplateJson,
    t.AllowedTriggerTypes    = v.AllowedTriggerTypes,
    t.Availability           = v.Availability,
    t.DisplayName            = v.DisplayName,
    t.IconKey                = v.IconKey
FROM dbo.IntegrationTargets AS t
JOIN (
    VALUES
        (N'sink-blob', N'{}',                N'{}',  N'Event',     N'User', N'Blob Storage', N'archive'),
        (N'sink-countblobs', N'{"container":"icp-sink-blob"}',                N'{}',  N'Schedule,OnDemand',     N'User', N'Blob Storage', N'archive')
) AS v(Name, ParametersTemplateJson, SecretsTemplateJson, AllowedTriggerTypes, Availability, DisplayName, IconKey)
    ON t.Name = v.Name;

-- 2) INSERT missing rows (idempotent)
INSERT INTO dbo.IntegrationTargets (Name, ParametersTemplateJson, SecretsTemplateJson, AllowedTriggerTypes, Availability, DisplayName, IconKey)
SELECT
    v.Name,
    v.ParametersTemplateJson,
    v.SecretsTemplateJson,
    v.AllowedTriggerTypes,
    v.Availability,
    v.DisplayName,
    v.IconKey
FROM (
    VALUES
        (N'sink-blob', N'{}',         N'{}',              N'Event',N'User', N'Blob Storage', N'archive'),
        (N'sink-countblobs', N'{"container":"icp-sink-blob"}',                N'{}',  N'Schedule,OnDemand',     N'User', N'Blob Storage', N'archive')
) AS v(Name, ParametersTemplateJson, SecretsTemplateJson, AllowedTriggerTypes, Availability, DisplayName, IconKey)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.IntegrationTargets t
    WHERE t.Name = v.Name
);


     -- Seed schedule time zones lookup table
     -- Assumes table: dbo.ScheduleTimeZones (Id nvarchar(100) PK, DisplayName nvarchar(200), Enabled bit, SortOrder int)
 
     SET NOCOUNT ON;
 
     MERGE dbo.ScheduleTimeZones AS tgt
     USING (VALUES
         (N'FLE Standard Time',     N'Helsinki (UTC+2 / UTC+3)', CAST(1 AS bit), 10),
         (N'GMT Standard Time',     N'London (UTC+0 / UTC+1)',   CAST(1 AS bit), 20),
         (N'W. Europe Standard Time', N'Berlin (UTC+1 / UTC+2)', CAST(1 AS bit), 30)
     ) AS src (Id, DisplayName, Enabled, SortOrder)
     ON tgt.Id = src.Id
     WHEN MATCHED THEN
         UPDATE SET
             tgt.DisplayName = src.DisplayName,
             tgt.Enabled = src.Enabled,
             tgt.SortOrder = src.SortOrder
     WHEN NOT MATCHED BY TARGET THEN
         INSERT (Id, DisplayName, Enabled, SortOrder)
         VALUES (src.Id, src.DisplayName, src.Enabled, src.SortOrder);
 
     -- Optional: disable any other existing time zones not in the seed set
     -- UPDATE dbo.ScheduleTimeZones
     -- SET Enabled = 0
     -- WHERE Id NOT IN (N'FLE Standard Time', N'GMT Standard Time', N'W. Europe Standard Time');
 