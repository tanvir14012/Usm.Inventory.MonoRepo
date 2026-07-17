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
            ["others"] =
            [
                new SidebarSeedItem("store-management", "store-management", "Store Management", 10, "inventory_2", true),
                new SidebarSeedItem("repair-maintenance", "repair-maintenance", "Repair & Maintenance", 20, "build", true),
                new SidebarSeedItem("salvage", "salvage", "Salvage", 30, "recycling", true),
                new SidebarSeedItem("budget-planning", "budget-planning", "Budget & Planning", 40, "account_balance_wallet", true),
                new SidebarSeedItem("inspectorate", "inspectorate", "Inspectorate", 50, "manage_search", true),
                new SidebarSeedItem("administration", "administration", "Administration", 60, "admin_panel_settings", true),
                new SidebarSeedItem("communication", "communication", "Communication", 70, "chat", true),
                new SidebarSeedItem("dms", "dms", "DMS", 80, "description", true)
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
