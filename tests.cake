#addin nuget:https://myget.org/f/cake-sqlserver/?package=Cake.SqlServer
//#r "./build-results/bin/Cake.SqlServer.dll"

var target = Argument("target", "Default");


Setup(context =>
{
    Information("Starting integration tests");
});

Teardown(context =>
{
    Information("Finished with integration tests");
});



Task("Create-LocalDB")
     .Does(() =>
     {
        // creates and starts instance
        // you don't need to start the instance separately
        //LocalDbCreateInstance("Cake-Test", LocalDbVersion.V12);
         LocalDbCreateInstance("Cake-Test");
     });


Task("Start-LocalDB")
     .Does(() =>
     {
        LocalDbStartInstance("Cake-Test");
    });


Task("Stop-LocalDB")
     .Does(() =>
     {
        LocalDbStopInstance("Cake-Test");
    });


Task("Delete-LocalDB")
     .Does(() =>
     {
        LocalDbDeleteInstance("Cake-Test");
    });

Task("Debug")
    .Does(() => 
    {
        Information("Welcome to debug");
    });


Task("Database-Operations")
	.Does(() => 
	{
	    var masterConnectionString = @"data source=(LocalDb)\v12.0;";
	    var connectionString = @"data source=(LocalDb)\v12.0;Database=OpsCakeTest";

		var dbName = "OpsCakeTest";

		// drop the db to be sure
		DropDatabase(masterConnectionString, dbName);
			
		// first create database
		CreateDatabase(masterConnectionString, dbName);

		// try the database again
		CreateDatabaseIfNotExists(masterConnectionString, dbName);
			
		// and recreate the db again
		DropAndCreateDatabase(masterConnectionString, dbName);

		// and create some tables
		ExecuteSqlCommand(connectionString, "create table dbo.Products(id int null)");
			
		// and execute sql from a file 
		ExecuteSqlFile(connectionString, "install.sql");

		// then drop the database
		DropDatabase(masterConnectionString, dbName);
	});


Task("SqlConnection")
	.Does(() => {
		var masterConnectionString = @"data source=(LocalDb)\v12.0;";
	    var connectionString = @"data source=(LocalDb)\v12.0;Database=OpenConnection";

		var dbName = "OpenConnection";

		CreateDatabase(masterConnectionString, dbName);

		using (var connection = OpenSqlConnection(connectionString))
		{
			ExecuteSqlCommand(connection, "create table dbo.Products(id int null)");
			
			ExecuteSqlFile(connection, "install.sql");		
		}

	})
	.Finally(() =>
	{  
		// Cleanup
		DropDatabase(@"data source=(LocalDb)\v12.0;", "OpenConnection");
	});


Task("SqlTimeout")
	.Does(() => {
		SetSqlCommandTimeout(3);
		using (var connection = OpenSqlConnection(@"Data Source=(LocalDb)\v12.0;"))
		{
			ExecuteSqlCommand(connection, "WAITFOR DELAY '00:00:02'");
		}
	});


Task("Restore-Database")
	.Does(() => {
		var connString = @"data source=(LocalDb)\v12.0";

		var backupFilePath = new FilePath(@".\src\Tests\multiFileBackup.bak");
		backupFilePath = backupFilePath.MakeAbsolute(Context.Environment);

		RestoreSqlBackup(connString, backupFilePath); 
	
		RestoreSqlBackup(connString, backupFilePath, new RestoreSqlBackupSettings() 
			{
				NewDatabaseName = "RestoredFromTest.Cake",
				NewStorageFolder = new DirectoryPath(System.IO.Path.GetTempPath()), // place files in special location
			}); 
	})
	.Finally(() =>
	{  
		// Cleanup
		DropDatabase(@"data source=(LocalDb)\v12.0", "RestoredFromTest.Cake");
		DropDatabase(@"data source=(LocalDb)\v12.0", "CakeRestoreTest");
	});


Task("Create-Bacpac")
	.Does(() =>{
		var connString = @"data source=(LocalDb)\v12.0";

		var dbName = "ForBacpac";

		CreateDatabase(connString, dbName);

		CreateBacpacFile(connString, dbName, new FilePath(@".\ForBacpac.bacpac"));
	})
	.Finally(() =>
	{  
		// Cleanup
		DropDatabase(@"data source=(LocalDb)\v12.0", "ForBacpac");
		if(FileExists(@".\ForBacpac.bacpac"))
		{
			DeleteFile(@".\ForBacpac.bacpac");
		}
	});


Task("Restore-From-Bacpac")
	.Does(() =>{
		var connString = @"data source=(LocalDb)\v12.0";

		var dbName = "FromBacpac";

		var file = new FilePath(@".\src\Tests\Nsaga.bacpac");
		RestoreBacpac(connString, dbName, file);
	})
	.Finally(() =>
	{  
		// Cleanup
		DropDatabase(@"data source=(LocalDb)\v12.0", "FromBacpac");
	});


Task("Default")
    .IsDependentOn("Create-LocalDB")
    .IsDependentOn("Start-LocalDB")
    .IsDependentOn("Stop-LocalDB")
    .IsDependentOn("Delete-LocalDB")
    .IsDependentOn("Database-Operations")
    .IsDependentOn("SqlConnection")
    .IsDependentOn("SqlTimeout")
    .IsDependentOn("Restore-Database")
    .IsDependentOn("Create-Bacpac")
    .IsDependentOn("Restore-From-Bacpac")
    ;    

RunTarget(target);