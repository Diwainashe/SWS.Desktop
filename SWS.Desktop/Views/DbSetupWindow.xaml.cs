using System.Text;
using System.Windows;
using SWS.Desktop.Services;

namespace SWS.Desktop.Views;

/// <summary>
/// Allows the user to configure a SQL Server connection string.
/// Saves it to user-scoped app settings (Properties.Settings) and can initialize the DB.
/// </summary>
public partial class DbSetupWindow : Window
{
    public DbSetupWindow()
    {
        InitializeComponent();

        // Sensible defaults for onsite installs (SQL Express)
        ServerBox.Text = @".\SQLEXPRESS";
        DatabaseBox.Text = "SWS";
    }

    /// <summary>
    /// Show/hide SQL authentication fields based on auth mode.
    /// </summary>
    private void WindowsAuthChanged(object sender, RoutedEventArgs e)
    {
        SqlAuthPanel.Visibility = WindowsAuthBox.IsChecked == true
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    /// Builds a SQL Server connection string from the UI fields.
    /// </summary>
    private string BuildConnectionString()
    {
        var server = ServerBox.Text.Trim();
        var db = DatabaseBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(server))
            throw new InvalidOperationException("Server is required.");

        if (string.IsNullOrWhiteSpace(db))
            throw new InvalidOperationException("Database name is required.");

        var sb = new StringBuilder();
        sb.Append($"Server={server};Database={db};");

        if (WindowsAuthBox.IsChecked == true)
        {
            sb.Append("Trusted_Connection=True;");
        }
        else
        {
            var user = UserBox.Text.Trim();
            var pass = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("SQL username is required.");

            sb.Append($"User Id={user};Password={pass};");
        }

        // Practical defaults for industrial sites:
        sb.Append("TrustServerCertificate=True;");
        sb.Append("Encrypt=False;");

        return sb.ToString();
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cs = BuildConnectionString();
            var (ok, msg) = await SqlConnectionTester.TestAsync(cs);

            MessageBox.Show(
                msg,
                ok ? "Success" : "Failed",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SaveInit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cs = BuildConnectionString();

            // 1) Test connection before saving
            var (ok, msg) = await SqlConnectionTester.TestAsync(cs);
            if (!ok)
            {
                MessageBox.Show(msg, "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2) Save to user-scoped settings (persists per Windows user)
            Properties.Settings.Default.SwsConnectionString = cs;
            Properties.Settings.Default.Save();

            // 3) Initialize DB (creates DB if missing + applies migrations)
            DatabaseBootstrapper.EnsureDatabaseMigrated(cs);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Setup failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
