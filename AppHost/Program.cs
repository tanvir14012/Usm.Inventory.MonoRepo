var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithPgAdmin();

var appDb = postgres.AddDatabase("usmdb");

var rabbitMq = builder.AddRabbitMQ("rabbitmq", port: 5672)
    .WithManagementPlugin(port: 15672);

var redis = builder.AddRedis("redis", port: 6379);

// Observability
builder.AddContainer("prometheus", "prom/prometheus")
    .WithHttpEndpoint(targetPort: 9090, port: 9090, name: "http");

builder.AddContainer("loki", "grafana/loki")
    .WithHttpEndpoint(targetPort: 3100, port: 3100, name: "http");

builder.AddContainer("grafana", "grafana/grafana")
    .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "http");

builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithHttpEndpoint(targetPort: 16686, port: 16686, name: "ui")
    .WithEndpoint(targetPort: 4317, port: 4319, name: "otlp-grpc", scheme: "http");

builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithEndpoint(targetPort: 4317, port: 4317, name: "grpc", scheme: "http")
    .WithHttpEndpoint(targetPort: 4318, port: 4318, name: "http")
    .WithHttpEndpoint(targetPort: 8889, port: 8889, name: "metrics");

// Core identity/access cluster
var identity = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithHttpEndpoint(port: 7101, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var iam = builder.AddProject<Projects.Iam_Api>("iam-api")
    .WithHttpEndpoint(port: 7102, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var administration = builder.AddProject<Projects.Administration_Api>("administration-api")
    .WithHttpEndpoint(port: 7103, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Inventory operations cluster
var storeHouse = builder.AddProject<Projects.StoreHouse_Api>("storehouse-api")
    .WithHttpEndpoint(port: 7104, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var issueReceipt = builder.AddProject<Projects.IssueReceipt_Api>("issuereceipt-api")
    .WithHttpEndpoint(port: 7106, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var repairMaintenance = builder.AddProject<Projects.RepairMaintenance_Api>("repairmaintenance-api")
    .WithHttpEndpoint(port: 7109, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var salvage = builder.AddProject<Projects.Salvage_Api>("salvage-api")
    .WithHttpEndpoint(port: 7108, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var trafficSecurity = builder.AddProject<Projects.TrafficSecurity_Api>("trafficsecurity-api")
    .WithHttpEndpoint(port: 7107, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Supporting services cluster
var documentShare = builder.AddProject<Projects.DocumentShare_Api>("documentshare-api")
    .WithHttpEndpoint(port: 7105, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var communication = builder.AddProject<Projects.Communication_Api>("communication-api")
    .WithHttpEndpoint(port: 7111, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var inspectorate = builder.AddProject<Projects.Inspectorate_Api>("inspectorate-api")
    .WithHttpEndpoint(port: 7112, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Planning/analytics cluster
var budgetPlanning = builder.AddProject<Projects.BudgetPlanning_Api>("budgetplanning-api")
    .WithHttpEndpoint(port: 7110, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var reporting = builder.AddProject<Projects.Reporting_Api>("reporting-api")
    .WithHttpEndpoint(port: 7113, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var procurement = builder.AddProject<Projects.Procurement_Api>("procurement-api")
    .WithHttpEndpoint(port: 7114, isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Gateway (depends on all APIs)
builder.AddProject<Projects.ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, isProxied: false)
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