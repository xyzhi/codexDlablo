param(
    [string]$ProjectRoot = "E:\xyz\codexDlabloB"
)

$docsRoot = Join-Path $ProjectRoot "Docs"
$configRoot = Join-Path $ProjectRoot "Assets\Resources\Configs"

function Convert-CsvToJsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$CsvPath,
        [Parameter(Mandatory = $true)][string]$JsonPath,
        [Parameter(Mandatory = $true)][string]$RootKey,
        [Parameter(Mandatory = $true)][hashtable]$FieldMap,
        [string[]]$IntFields = @()
    )

    if (-not (Test-Path $CsvPath)) {
        return
    }

    $rows = Import-Csv $CsvPath
    $items = @()

    foreach ($row in $rows) {
        $item = [ordered]@{}
        foreach ($sourceField in $FieldMap.Keys) {
            $targetField = $FieldMap[$sourceField]
            $value = $row.$sourceField
            if ($IntFields -contains $sourceField) {
                $item[$targetField] = [int]$value
            }
            else {
                $item[$targetField] = [string]$value
            }
        }
        $items += $item
    }

    $payload = [ordered]@{}
    $payload[$RootKey] = $items
    $payload | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $JsonPath
    Write-Host "Synced $CsvPath -> $JsonPath"
}

Convert-CsvToJsonFile `
    -CsvPath (Join-Path $docsRoot "Skill.csv") `
    -JsonPath (Join-Path $configRoot "SkillDatabase.json") `
    -RootKey "skills" `
    -FieldMap @{
        ID = "Id"
        Name = "Name"
        Element = "Element"
        Quality = "Quality"
        Category = "Category"
        TargetType = "TargetType"
        MPCost = "MPCost"
        Power = "Power"
        Duration = "Duration"
        EffectType = "EffectType"
        Description = "Description"
        Notes = "Notes"
    } `
    -IntFields @("MPCost", "Power", "Duration")

Convert-CsvToJsonFile `
    -CsvPath (Join-Path $docsRoot "StageBalance.csv") `
    -JsonPath (Join-Path $configRoot "StageBalanceDatabase.json") `
    -RootKey "stageBalances" `
    -FieldMap @{
        Stage = "Stage"
        EnemyHPBonus = "EnemyHPBonus"
        EnemyATKBonus = "EnemyATKBonus"
        EnemyDEFBonus = "EnemyDEFBonus"
        EnemyMPBonus = "EnemyMPBonus"
        EnemyEquipmentCount = "EnemyEquipmentCount"
        PlayerHPBonus = "PlayerHPBonus"
        PlayerATKBonus = "PlayerATKBonus"
        PlayerDEFBonus = "PlayerDEFBonus"
        PlayerMPBonus = "PlayerMPBonus"
        Notes = "Notes"
    } `
    -IntFields @(
        "Stage",
        "EnemyHPBonus",
        "EnemyATKBonus",
        "EnemyDEFBonus",
        "EnemyMPBonus",
        "EnemyEquipmentCount",
        "PlayerHPBonus",
        "PlayerATKBonus",
        "PlayerDEFBonus",
        "PlayerMPBonus"
    )
