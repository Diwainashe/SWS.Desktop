using System;
using System.Windows;
using Microsoft.Data.SqlClient; // Preferred for .NET 8 (NuGet: Microsoft.Data.SqlClient)

namespace SWS.Desktop.Views;

/// <summary>
/// Simple DB setup window:
/// - user enters Server + Database
/// - choose Windows Auth or SQL Login
/// - can Test connection
/// - can Save (writes to Properties.Settings)
/// 
/// NOTE:
/// This window should only collect & save settings.
/// App startup will run migrations + seeding AFTER this window returns OK.
/// </summary>
public partial class DbSetupWindow : Window
{
    public DbSetupWindow()
    {
        InitializeComponent();

        // Load existing saved values (if any) so user can edit instead of retyping.
        LoadFromSettings();

        // Apply UI enable/disable based on the checkbox state
        ApplyAuthModeToUi();
    }

    /// <summary>
    /// Called when the "Use Windows Authentication" checkbox changes.
    /// Enables/disables username/password fields.
    /// </summary>
    private void WindowsAuthChanged(object sender, RoutedEventArgs e)
    {
        ApplyAuthModeToUi();
    }

    private void ApplyAuthModeToUi()
    {
        // In WPF, IsChecked is bool? (nullable). Treat null as false.
        bool windowsAuth = WindowsAuthCheckBox.IsChecked == true;

        UsernameTextBox.IsEnabled = !windowsAuth;
        PasswordBox.IsEnabled = !windowsAuth;

        // Optional: visually clear/keep values
        // If you want to keep the typed SQL login for later toggles, do NOT clear.
        // If you'd rather avoid confusion, you can clear when switching to Windows auth:
        // if (windowsAuth) { UsernameTextBox.Text = ""; PasswordBox.Password = ""; }
    }

    /// <summary>
    /// Test button: attempts to open SQL connection using current inputs.
    /// </summary>
    private void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string cs = BuildConnectionStringFromUi();

            using var conn = new SqlConnection(cs);
            conn.Open();

            MessageBox.Show(
                "Connection OK.",
                "SWS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Connection FAILED:\n\n" + ex.Message,
                "SWS",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Save & Initialize:
    /// - validates inputs
    /// - writes the connection string to Properties.Settings.Default.SwsConnectionString
    /// - closes dialog with DialogResult = true
    /// 
    /// App startup will then call DatabaseBootstrapper.EnsureDatabaseReady(cs).
    /// </summary>
    private void SaveInit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string cs = BuildConnectionStringFromUi();

            // Persist for future runs
            Properties.Settings.Default.SwsConnectionString = cs;

            // Optional: persist helper fields (if you add them to Settings.settings later)
            // Properties.Settings.Default.SwsServer = ServerBox.Text.Trim();
            // Properties.Settings.Default.SwsDatabase = DatabaseBox.Text.Trim();
            // Properties.Settings.Default.UseWindowsAuth = WindowsAuthCheckBox.IsChecked == true;

            Properties.Settings.Default.Save();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Could not save settings:\n\n" + ex.Message,
                "SWS",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Cancel: close dialog and tell caller startup should stop.
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Builds a SQL Server connection string based on current UI values.
    /// </summary>
    private string BuildConnectionStringFromUi()
    {
        string server = (ServerBox.Text ?? string.Empty).Trim();
        string database = (DatabaseBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(server))
            throw new InvalidOperationException("Server is required.");

        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("Database name is required.");

        bool windowsAuth = WindowsAuthCheckBox.IsChecked == true;

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            TrustServerCertificate = true,
            Encrypt = false, // local / plant networks: usually false; set true if needed
        };

        if (windowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            string user = (UsernameTextBox.Text ?? string.Empty).Trim();
            string pass = PasswordBox.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("Username is required for SQL Login.");

            if (string.IsNullOrWhiteSpace(pass))
                throw new InvalidOperationException("Password is required for SQL Login.");

            builder.UserID = user;
            builder.Password = pass;
            builder.IntegratedSecurity = false;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Pre-fills the UI based on the currently saved connection string (if it exists).
    /// This makes the window friendlier when the user is just editing settings.
    /// </summary>
    private void LoadFromSettings()
    {
        string cs = Properties.Settings.Default.SwsConnectionString;

        if (string.IsNullOrWhiteSpace(cs))
        {
            // Reasonable defaults
            ServerBox.Text = @"(localdb)\MSSQLLocalDB";
            DatabaseBox.Text = "SWS";
            WindowsAuthCheckBox.IsChecked = true;
            return;
        }

        try
        {
            var b = new SqlConnectionStringBuilder(cs);

            ServerBox.Text = b.DataSource;
            DatabaseBox.Text = b.InitialCatalog;

            bool isWindowsAuth = b.IntegratedSecurity;
            WindowsAuthCheckBox.IsChecked = isWindowsAuth;

            // If it was SQL auth, show username (never prefill password)
            if (!isWindowsAuth && !string.IsNullOrWhiteSpace(b.UserID))
                UsernameTextBox.Text = b.UserID;
        }
        catch
        {
            // If parsing fails, just leave whatever is on screen / defaults.
            ServerBox.Text = @"(localdb)\MSSQLLocalDB";
            DatabaseBox.Text = "SWS";
            WindowsAuthCheckBox.IsChecked = true;
        }
    }
}