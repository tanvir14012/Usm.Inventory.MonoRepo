var builder = DistributedApplication.CreateBuilder(args);

// Load .env from repo root and expose a helper for port lookup with fallback
var envFile = Path.Combine(builder.AppHostDirectory, "..", ".env");
var envVars = File.Exists(envFile)
    ? File.ReadAllLines(envFile)
        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#') && l.Contains('='))
        .Select(l => l.Split('=', 2))
        .ToDictionary(p => p[0].Trim(), p => p[1].Trim())
    : new Dictionary<string, string>();

int Port(string key, int fallback) =>
    envVars.TryGetValue(key, out var v) && int.TryParse(v, out var p) ? p : fallback;
string Env(string key, string fallback) =>
    envVars.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

var postgresUser = Env("POSTGRES_USER", "usm_admin");
var postgresPassword = Env("POSTGRES_PASSWORD", "usm_admin_dev");
var postgresDatabase = Env("POSTGRES_DB", "usm_inventory");
var postgresUserParameter = builder.AddParameter("postgres-user", postgresUser);
var postgresPasswordParameter = builder.AddParameter("postgres-password", postgresPassword, secret: true);

// Infrastructure
var postgres = builder.AddPostgres(
    "postgres",
    userName: postgresUserParameter,
    password: postgresPasswordParameter,
    port: Port("POSTGRES_PORT", 5432))
    .WithPgAdmin();

var appDb = postgres.AddDatabase("usmdb", postgresDatabase);

var rabbitMq = builder.AddRabbitMQ("rabbitmq", port: Port("RABBITMQ_PORT", 5672))
    .WithManagementPlugin(port: Port("RABBITMQ_MANAGEMENT_PORT", 15672));

var redis = builder.AddRedis("redis", port: Port("REDIS_PORT", 6379));

// Observability
builder.AddContainer("prometheus", "prom/prometheus")
    .WithHttpEndpoint(targetPort: 9090, port: Port("PROMETHEUS_PORT", 9090), name: "http");

builder.AddContainer("loki", "grafana/loki")
    .WithHttpEndpoint(targetPort: 3100, port: Port("LOKI_PORT", 3100), name: "http");

builder.AddContainer("grafana", "grafana/grafana")
    .WithHttpEndpoint(targetPort: 3000, port: Port("GRAFANA_PORT", 3000), name: "http");

builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithHttpEndpoint(targetPort: 16686, port: Port("JAEGER_UI_PORT", 16686), name: "ui")
    .WithEndpoint(targetPort: 4317, port: Port("JAEGER_OTLP_GRPC_PORT", 4319), name: "otlp-grpc", scheme: "http");

builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithEndpoint(targetPort: 4317, port: Port("OTEL_COLLECTOR_GRPC_PORT", 4317), name: "grpc", scheme: "http")
    .WithHttpEndpoint(targetPort: 4318, port: Port("OTEL_COLLECTOR_HTTP_PORT", 4318), name: "http")
    .WithHttpEndpoint(targetPort: 8889, port: Port("OTEL_COLLECTOR_METRICS_PORT", 8889), name: "metrics");

// Core identity/access cluster
var identity = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithHttpEndpoint(port: Port("IDENTITY_API_PORT", 7101), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var iam = builder.AddProject<Projects.Iam_Api>("iam-api")
    .WithHttpEndpoint(port: Port("IAM_API_PORT", 7102), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var administration = builder.AddProject<Projects.Administration_Api>("administration-api")
    .WithHttpEndpoint(port: Port("ADMINISTRATION_API_PORT", 7103), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Inventory operations cluster
var storeHouse = builder.AddProject<Projects.StoreHouse_Api>("storehouse-api")
    .WithHttpEndpoint(port: Port("STOREHOUSE_API_PORT", 7104), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var issueReceipt = builder.AddProject<Projects.IssueReceipt_Api>("issuereceipt-api")
    .WithHttpEndpoint(port: Port("ISSUERECEIPT_API_PORT", 7106), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var repairMaintenance = builder.AddProject<Projects.RepairMaintenance_Api>("repairmaintenance-api")
    .WithHttpEndpoint(port: Port("REPAIRMAINTENANCE_API_PORT", 7109), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var salvage = builder.AddProject<Projects.Salvage_Api>("salvage-api")
    .WithHttpEndpoint(port: Port("SALVAGE_API_PORT", 7108), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var trafficSecurity = builder.AddProject<Projects.TrafficSecurity_Api>("trafficsecurity-api")
    .WithHttpEndpoint(port: Port("TRAFFICSECURITY_API_PORT", 7107), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Supporting services cluster
var documentShare = builder.AddProject<Projects.DocumentShare_Api>("documentshare-api")
    .WithHttpEndpoint(port: Port("DOCUMENTSHARE_API_PORT", 7105), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var communication = builder.AddProject<Projects.Communication_Api>("communication-api")
    .WithHttpEndpoint(port: Port("COMMUNICATION_API_PORT", 7111), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var inspectorate = builder.AddProject<Projects.Inspectorate_Api>("inspectorate-api")
    .WithHttpEndpoint(port: Port("INSPECTORATE_API_PORT", 7112), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Planning/analytics cluster
var budgetPlanning = builder.AddProject<Projects.BudgetPlanning_Api>("budgetplanning-api")
    .WithHttpEndpoint(port: Port("BUDGETPLANNING_API_PORT", 7110), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var reporting = builder.AddProject<Projects.Reporting_Api>("reporting-api")
    .WithHttpEndpoint(port: Port("REPORTING_API_PORT", 7113), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

var procurement = builder.AddProject<Projects.Procurement_Api>("procurement-api")
    .WithHttpEndpoint(port: Port("PROCUREMENT_API_PORT", 7114), isProxied: false)
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(appDb)
    .WaitFor(rabbitMq);

// Gateway (depends on all APIs)
builder.AddProject<Projects.ApiGateway>("gateway")
    .WithHttpEndpoint(port: Port("API_GATEWAY_PORT", 5000), isProxied: false)
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