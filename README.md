# DbUp.Downgrade
Extension of [DbUp](https://github.com/DbUp/DbUp) engine that allows automatic downgrade / rollback / revert of scripts.

The library plugs into [DbUp pipeline](https://github.com/DbUp/DbUp/blob/master/docs/usage.md) extending the Configuration options. **UpgradeEngineBuilderExtensions** class adds few simple methods that allow to developers to extend the **SchemaVersion** table through abstraction providing custom SQL Queries over DbUp supported databases. **SchemaVersion** now supports additional column *DowgradeScript* that will store the script that will revert executed upgrade script. The basic idea is to store the sql query it self in the **SchemaVersion** and in case of *Rollback* to be able to perform downgrade of the Database to perviews version. Currently the only provided implementation is SQL Server based but it can be easily extended to all [DbUp supported databases](https://github.com/DbUp/DbUp/blob/master/docs/supported-databases.md)

## NOTE
All existing DbUp projects can integrate the library. It will automatically make the required schema changes in [DbUp Journal table](https://github.com/DbUp/DbUp/blob/master/docs/more-info/journaling.md) in order to operate correctly. All newly added upgrade scripts can have a corresponding downgrade script.

## Usage
1. Install ```Install-Package DbUp.Downgrade```

Aftre successful install of the package you can start using it by:

Include the namespace 
```csharp 
using DbUp.Downgrade;
```

This namespace will add new extensions to DbUp configuration builder:
```csharp
var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), DowngradeScriptsSettings.FromSuffix())
                .LogToConsole();
```

If you are using scripts embedded in the executing assembly you can directly use ```WithScriptsAndDowngradeScriptsEmbeddedInAssembly``` method. It will require an instance of ```DowngradeScriptsSettings``` class.
The settings has two static methods for fast and easy setup ```FromSuffix(string suffixName)``` and ```FromFolder(string folderName)```. By using them the code will rely that you have either downgrade scripts in a folder named same as the upgrade scripts:
    .
    ├── ...
    ├── Scripts                             # The scripts that will perform the upgrade of the Database
    │   ├── Script0001 - Create tables.sql          
    │   ├── Script0002 - Default feed.sql
    │   └── Script0003 - Settings.sql
    ├── DowngradeScripts                    # Scripts that will revert the changes made from the Upgrade Scripts
    │   ├── Script0003 - Settings.sql       # NOTE: Must be with same name as the upgrade script
    └── ...

After the folder structure and naming convention is placed correctly you can build the ```DowngradeEnabledUpgradeEngine```.
```csharp
var upgrader = upgradeEngineBuilder.BuildWithDowngrade(true);
```
The boolean parameter indicates will on detected difference an automated downgrade will be executed. If this is set to false you will need to manually call ```upgrader.PerformDowngrade()``` method and handle the returned **DatabaseUpgradeResult**.

If your implementation is not based on scripts embedded in the executing assembly you will need to provide corresponding ```upgradeScriptProvider``` and ```downgradeScriptProvider```. 
Both scenarios are showcased in the [SampleApplication](https://github.com/asimeonov/DbUp.Downgrade/tree/master/src/SampleApplication) of the project