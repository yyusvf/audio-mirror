namespace AudioMirror.Ui;

/// <summary>
/// Überschrift eines Listenabschnitts mit anschließender Trennlinie, die den restlichen Platz
/// füllt - trennt „Verbunden“ optisch klar von „Getrennt“.
/// </summary>
internal sealed class SectionHeader : Panel
{
    private readonly Label caption;

    public SectionHeader(string text)
    {
        caption = new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = SystemColors.Highlight,
            Location = new Point(8, 3),
        };

        Dock = DockStyle.Top;
        Margin = Padding.Empty;
        BackColor = Color.Transparent;
        Controls.Add(caption);
        ApplyMetrics();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyMetrics();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        int y = caption.Top + (caption.Height / 2);
        int left = caption.Right + 8;
        int right = ClientSize.Width - 8;
        if (right <= left)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(90, SystemColors.Highlight));
        e.Graphics.DrawLine(pen, left, y, right, y);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    private void ApplyMetrics() => Height = Font.Height + 10;
}
