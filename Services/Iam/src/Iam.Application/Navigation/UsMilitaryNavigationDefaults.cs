namespace Iam.Application.Navigation;

internal static class UsMilitaryNavigationDefaults
{
    private static readonly Dictionary<string, IReadOnlyList<SidebarSeedItem>> SidebarByModuleKey =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] =
            [
                new SidebarSeedItem("situation-overview", "situation-overview", "Situation Overview", 10, "dashboard", true),
                new SidebarSeedItem("readiness-snapshot", "readiness-snapshot", "Readiness Snapshot", 20, "military_tech", true),
                new SidebarSeedItem("alert-feed", "alert-feed", "Alert Feed", 30, "notifications_active", true)
            ],
            ["procurement"] =
            [
                new SidebarSeedItem("requisition", "requisition", "Requisition", 10, "assignment", true),
                new SidebarSeedItem("vendor-vetting", "vendor-vetting", "Vendor Vetting", 20, "fact_check", true),
                new SidebarSeedItem("contract-awards", "contract-awards", "Contract Awards", 30, "gavel", true),
                new SidebarSeedItem("logistics-tracking", "logistics-tracking", "Logistics Tracking", 40, "local_shipping", true)
            ],
            ["issue-receipt"] =
            [
                new SidebarSeedItem("issue-request", "issue-request", "Issue Request", 10, "outbox", true),
                new SidebarSeedItem("receipt-confirmation", "receipt-confirmation", "Receipt Confirmation", 20, "inbox", true),
                new SidebarSeedItem("transfer-ledger", "transfer-ledger", "Transfer Ledger", 30, "receipt_long", true)
            ],
            ["traffic-security"] =
            [
                new SidebarSeedItem("convoy-routing", "convoy-routing", "Convoy Routing", 10, "alt_route", true),
                new SidebarSeedItem("gate-control", "gate-control", "Gate Control", 20, "security", true),
                new SidebarSeedItem("incident-reports", "incident-reports", "Incident Reports", 30, "report", true),
                new SidebarSeedItem("threat-advisory", "threat-advisory", "Threat Advisory", 40, "warning", true)
            ],
            ["store-management"] =
            [
                new SidebarSeedItem("stock-levels", "stock-levels", "Stock Levels", 10, "stacked_bar_chart", true),
                new SidebarSeedItem("reorder-alerts", "reorder-alerts", "Reorder Alerts", 20, "notification_important", true),
                new SidebarSeedItem("asset-cards", "asset-cards", "Asset Cards", 30, "badge", true)
            ],
            ["repair-maintenance"] =
            [
                new SidebarSeedItem("work-orders", "work-orders", "Work Orders", 10, "handyman", true),
                new SidebarSeedItem("maintenance-schedule", "maintenance-schedule", "Maintenance Schedule", 20, "calendar_month", true),
                new SidebarSeedItem("parts-usage", "parts-usage", "Parts Usage", 30, "precision_manufacturing", true)
            ],
            ["salvage"] =
            [
                new SidebarSeedItem("recovery-cases", "recovery-cases", "Recovery Cases", 10, "inventory", true),
                new SidebarSeedItem("disposal-records", "disposal-records", "Disposal Records", 20, "delete_forever", true)
            ],
            ["budget-planning"] =
            [
                new SidebarSeedItem("allocations", "allocations", "Allocations", 10, "payments", true),
                new SidebarSeedItem("spend-forecast", "spend-forecast", "Spend Forecast", 20, "trending_up", true),
                new SidebarSeedItem("budget-vs-actual", "budget-vs-actual", "Budget vs Actual", 30, "compare_arrows", true)
            ],
            ["inspectorate"] =
            [
                new SidebarSeedItem("audits", "audits", "Audits", 10, "search_check", true),
                new SidebarSeedItem("findings", "findings", "Findings", 20, "rule_folder", true),
                new SidebarSeedItem("corrective-actions", "corrective-actions", "Corrective Actions", 30, "fact_check", true)
            ],
            ["administration"] =
            [
                new SidebarSeedItem("departments", "departments", "Departments", 10, "corporate_fare", true),
                new SidebarSeedItem("roles", "roles", "Roles", 20, "admin_panel_settings", true),
                new SidebarSeedItem("users", "users", "Users", 30, "groups", true)
            ],
            ["communication"] =
            [
                new SidebarSeedItem("channels", "channels", "Channels", 10, "forum", true),
                new SidebarSeedItem("announcements", "announcements", "Announcements", 20, "campaign", true),
                new SidebarSeedItem("messages", "messages", "Messages", 30, "message", true)
            ],
            ["dms"] =
            [
                new SidebarSeedItem("documents", "documents", "Documents", 10, "description", true),
                new SidebarSeedItem("folders", "folders", "Folders", 20, "folder", true),
                new SidebarSeedItem("versions", "versions", "Versions", 30, "history", true)
            ]
        };

    public static IReadOnlyList<SidebarSeedItem> ForModule(string systemName, string menuId)
    {
        if (SidebarByModuleKey.TryGetValue(systemName.Trim(), out var bySystemName))
        {
            return bySystemName;
        }

        if (SidebarByModuleKey.TryGetValue(menuId.Trim(), out var byMenuId))
        {
            return byMenuId;
        }

        return [];
    }
}

internal sealed record SidebarSeedItem(
    string SystemName,
    string MenuId,
    string LocalizedName,
    int DisplayOrder,
    string MaterialIconName,
    bool IsActive);
