using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CourseToIcal.Core.Models;

namespace CourseToIcal.App.UI
{
    public sealed class ScheduleSettingsForm : Form
    {
        public ScheduleConfig Config { get; private set; }
        private TextBox nameBox;
        private DateTimePicker startPicker;
        private ComboBox weekStartBox;
        private NumericUpDown weeksBox;
        private NumericUpDown currentWeekBox;
        private readonly TextBox[] startTimes = new TextBox[11];
        private readonly TextBox[] endTimes = new TextBox[11];

        public ScheduleSettingsForm(ScheduleConfig source)
        {
            Config = Clone(source);
            Text = "CourseToIcal · 课表设置";
            Width = 720;
            Height = 660;
            MinimumSize = new Size(640, 560);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.StyleForm(this);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3, BackColor = UiTheme.Background };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            Controls.Add(root);
            root.Controls.Add(new Label { Text = "课表设置", AutoSize = true, Font = new Font(UiTheme.HeadingFont, FontStyle.Bold), ForeColor = UiTheme.Text, Dock = DockStyle.Fill }, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill, Appearance = TabAppearance.Normal };
            tabs.TabPages.Add(BuildSemesterTab());
            tabs.TabPages.Add(BuildPeriodsTab());
            root.Controls.Add(tabs, 0, 1);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var save = new Button { Text = "保存设置", Width = 120, Height = 44, DialogResult = DialogResult.OK };
            UiTheme.StyleButton(save, true, false);
            save.Click += (s, e) => { if (!Apply()) DialogResult = DialogResult.None; };
            var cancel = new Button { Text = "取消", Width = 90, Height = 44, Margin = new Padding(8, 0, 0, 0), DialogResult = DialogResult.Cancel };
            UiTheme.StyleButton(cancel, false, false);
            actions.Controls.Add(save); actions.Controls.Add(cancel);
            root.Controls.Add(actions, 0, 2);
            AcceptButton = save; CancelButton = cancel;
        }

        private TabPage BuildSemesterTab()
        {
            var page = new TabPage("学期信息") { BackColor = UiTheme.Surface, Padding = new Padding(18) };
            var panel = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 7, AutoSize = true, BackColor = UiTheme.Surface };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            nameBox = new TextBox { Text = Config.Name, Dock = DockStyle.Fill };
            startPicker = new DateTimePicker { Value = Config.SemesterStart, Format = DateTimePickerFormat.Short, Dock = DockStyle.Left, Width = 160 };
            weekStartBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 160 };
            weekStartBox.Items.AddRange(new object[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" });
            weekStartBox.SelectedIndex = Math.Max(0, Math.Min(6, Config.WeekStart - 1));
            weeksBox = new NumericUpDown { Minimum = 1, Maximum = 60, Value = Config.SemesterWeeks, Width = 100 };
            currentWeekBox = new NumericUpDown { Minimum = 1, Maximum = 60, Value = Config.CurrentWeek, Width = 100 };
            AddRow(panel, 0, "课表名称", nameBox);
            AddRow(panel, 1, "第一周起始日期", startPicker);
            AddRow(panel, 2, "每周起始日", weekStartBox);
            AddRow(panel, 3, "学期周数", weeksBox);
            AddRow(panel, 4, "当前周", currentWeekBox);
            panel.Controls.Add(UiTheme.HintLabel("第一周日期会与课程表中的周次一起用于计算实际日历日期。"), 0, 5);
            panel.SetColumnSpan(panel.GetControlFromPosition(0, 5), 2);
            page.Controls.Add(panel);
            return page;
        }

        private TabPage BuildPeriodsTab()
        {
            var page = new TabPage("上课时间") { BackColor = UiTheme.Surface, Padding = new Padding(18) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 13, BackColor = UiTheme.Surface };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layout.Controls.Add(UiTheme.SectionLabel("节次"), 0, 0);
            layout.Controls.Add(UiTheme.SectionLabel("开始"), 1, 0);
            layout.Controls.Add(UiTheme.SectionLabel("结束"), 2, 0);
            for (int i = 0; i < 11; i++)
            {
                layout.Controls.Add(new Label { Text = "第 " + (i + 1) + " 节", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = UiTheme.Text }, 0, i + 1);
                startTimes[i] = new TextBox { Text = Config.Periods[i].StartText, Width = 110, Anchor = AnchorStyles.Left };
                endTimes[i] = new TextBox { Text = Config.Periods[i].EndText, Width = 110, Anchor = AnchorStyles.Left };
                UiTheme.StyleTextInput(startTimes[i]);
                UiTheme.StyleTextInput(endTimes[i]);
                layout.Controls.Add(startTimes[i], 1, i + 1);
                layout.Controls.Add(endTimes[i], 2, i + 1);
            }
            page.Controls.Add(layout);
            return page;
        }

        private void AddRow(TableLayoutPanel panel, int row, string label, Control control)
        {
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = UiTheme.Text }, 0, row);
            UiTheme.StyleTextInput(control);
            panel.Controls.Add(control, 1, row);
        }

        private bool Apply()
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                MessageBox.Show(this, "课表名称不能为空。", "设置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            Config.Name = nameBox.Text.Trim();
            Config.SemesterStart = startPicker.Value.Date;
            Config.WeekStart = weekStartBox.SelectedIndex + 1;
            Config.SemesterWeeks = (int)weeksBox.Value;
            Config.CurrentWeek = Math.Min((int)currentWeekBox.Value, Config.SemesterWeeks);
            for (int i = 0; i < 11; i++)
            {
                TimeSpan start;
                TimeSpan end;
                if (!TimeSpan.TryParseExact(startTimes[i].Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out start) || !TimeSpan.TryParseExact(endTimes[i].Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out end))
                {
                    MessageBox.Show(this, "第 " + (i + 1) + " 节时间格式错误，请使用 HH:mm，例如 08:30。", "设置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (end <= start)
                {
                    MessageBox.Show(this, "第 " + (i + 1) + " 节的结束时间必须晚于开始时间。", "设置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                Config.Periods[i].Start = start;
                Config.Periods[i].End = end;
            }
            Config.Normalize();
            return true;
        }

        private static ScheduleConfig Clone(ScheduleConfig source)
        {
            source.Normalize();
            var copy = new ScheduleConfig { Name = source.Name, SemesterStart = source.SemesterStart, WeekStart = source.WeekStart, SemesterWeeks = source.SemesterWeeks, CurrentWeek = source.CurrentWeek };
            copy.Periods = source.Periods.Select(p => new PeriodSetting { Number = p.Number, StartText = p.StartText, EndText = p.EndText }).ToList();
            return copy;
        }
    }
}
