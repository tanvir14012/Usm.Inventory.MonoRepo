var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var appDb = postgres.AddDatabase("usmdb");

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// Core identity/access cluster
var identity = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var iam = builder.AddProject<Projects.Iam_Api>("iam-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var administration = builder.AddProject<Projects.Administration_Api>("administration-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Inventory operations cluster
var storeHouse = builder.AddProject<Projects.StoreHouse_Api>("storehouse-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var issueReceipt = builder.AddProject<Projects.IssueReceipt_Api>("issuereceipt-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var repairMaintenance = builder.AddProject<Projects.RepairMaintenance_Api>("repairmaintenance-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var salvage = builder.AddProject<Projects.Salvage_Api>("salvage-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var trafficSecurity = builder.AddProject<Projects.TrafficSecurity_Api>("trafficsecurity-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Supporting services cluster
var documentShare = builder.AddProject<Projects.DocumentShare_Api>("documentshare-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var communication = builder.AddProject<Projects.Communication_Api>("communication-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var inspectorate = builder.AddProject<Projects.Inspectorate_Api>("inspectorate-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Planning/analytics cluster
var budgetPlanning = builder.AddProject<Projects.BudgetPlanning_Api>("budgetplanning-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var reporting = builder.AddProject<Projects.Reporting_Api>("reporting-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var procurement = builder.AddProject<Projects.Procurement_Api>("procurement-api")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Gateway (depends on all APIs)
builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(identity)
    .WithReference(iam)
    .WithReference(administration)
    .WithReference(storeHouse)
    .WithReference(issueReceipt)
    .WithReference(repairMaintenance)
    .WithReference(salvage)
    .WithReference(trafficSecurity)
    .WithReference(documentShare)
    .WithReference(communication)
    .WithReference(inspectorate)
    .WithReference(budgetPlanning)
    .WithReference(reporting)
    .WithReference(procurement)
    .WaitFor(identity)
    .WaitFor(iam)
    .WaitFor(administration)
    .WaitFor(storeHouse)
    .WaitFor(issueReceipt)
    .WaitFor(repairMaintenance)
    .WaitFor(salvage)
    .WaitFor(trafficSecurity)
    .WaitFor(documentShare)
    .WaitFor(communication)
    .WaitFor(inspectorate)
    .WaitFor(budgetPlanning)
    .WaitFor(reporting)
    .WaitFor(procurement);

builder.Build().Run();