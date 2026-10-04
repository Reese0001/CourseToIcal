using System;
using System.Drawing;
using System.Windows.Forms;

namespace CourseToIcal.App.UI
{
    internal static class UiTheme
    {
        public static readonly Color Background = Color.FromArgb(240, 253, 250);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceMuted = Color.FromArgb(232, 241, 244);
        public static readonly Color Primary = Color.FromArgb(13, 148, 136);
        public static readonly Color PrimaryHover = Color.FromArgb(20, 184, 166);
        public static readonly Color Accent = Color.FromArgb(234, 88, 12);
        public static readonly Color AccentHover = Color.FromArgb(249, 115, 22);
        public static readonly Color Success = Color.FromArgb(4, 120, 87);
        public static readonly Color Text = Color.FromArgb(19, 78, 74);
        public static readonly Color MutedText = Color.FromArgb(71, 85, 105);
        public static readonly Color Border = Color.FromArgb(153, 246, 228);
        public static readonly Color Focus = Color.FromArgb(13, 148, 136);
        public static readonly Color Error = Color.FromArgb(185, 28, 28);

        public static Font BodyFont { get { return new Font("Microsoft YaHei UI", 10F); } }
        public static Font SmallFont { get { return new Font("Microsoft YaHei UI", 9F); } }
        public static Font HeadingFont { get { return new Font("Microsoft YaHei UI", 20F, FontStyle.Bold); } }

        public static void StyleForm(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = Text;
            form.Font = BodyFont;
            form.KeyPreview = true;
        }

        public static void StyleButton(Button button, bool primary, bool accent)
        {
            Color baseColor = accent ? Accent : Primary;
            Color hoverColor = accent ? AccentHover : PrimaryHover;
            if (!primary)
            {
                button.BackColor = Surface;
                button.ForeColor = Text;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = SurfaceMuted;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            }
            else
            {
                button.BackColor = baseColor;
                button.ForeColor = Color.Black;
                button.FlatAppearance.BorderColor = baseColor;
                button.FlatAppearance.MouseOverBackColor = hoverColor;
                button.FlatAppearance.MouseDownBackColor = hoverColor;
            }
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            button.Padding = new Padding(12, 4, 12, 4);
            button.MinimumSize = new Size(0, 40);
            button.Enter += (s, e) => { button.FlatAppearance.BorderColor = Focus; button.FlatAppearance.BorderSize = 2; };
            button.Leave += (s, e) => { button.FlatAppearance.BorderColor = primary ? baseColor : Border; button.FlatAppearance.BorderSize = 1; };
        }

        public static void StyleTextInput(Control control)
        {
            control.BackColor = Surface;
            control.ForeColor = Text;
            control.Font = BodyFont;
        }

        public static Label SectionLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, ForeColor = Text, Font = new Font(BodyFont, FontStyle.Bold), Margin = new Padding(0, 0, 0, 4) };
        }

        public static Label HintLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, ForeColor = MutedText, Font = SmallFont, Margin = new Padding(0, 0, 0, 4) };
        }
    }
}
