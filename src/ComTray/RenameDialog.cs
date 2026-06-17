namespace ComTray;

static class RenameDialog
{
    public static string? Show(string title, string subtitle, string current)
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(400, 150)
        };

        var info = new Label
        {
            Left = 14,
            Top = 14,
            Width = 372,
            Height = 40,
            Text = subtitle
        };

        var box = new TextBox
        {
            Left = 14,
            Top = 62,
            Width = 372,
            Text = current,
            PlaceholderText = "e.g. Left USB C, Front hub port 3"
        };

        var ok = new Button { Text = "Save", Left = 230, Top = 100, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 311, Top = 100, Width = 75, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange([info, box, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        box.SelectAll();

        return form.ShowDialog() == DialogResult.OK ? box.Text : null;
    }
}
