namespace RevealSdk.Server

{
    // ****
    // Simple class to hold the dashboard name and title from the
    // App.MapGet("/dashboards/names", () => in Program.cs
    // ****
    public class DashboardNames
    {
        public string? DashboardFileName { get; set; }
        public string? DashboardTitle { get; set; }
    }
}