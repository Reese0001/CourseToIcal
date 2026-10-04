using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CourseToIcal.Core.Models;

namespace CourseToIcal.App.UI
{
    public sealed class ScheduleSettingsForm : Form
    {
        public ScheduleConfig Config { get; private set; }
        private readonly TextBox nameBox;
        private readonly DateTimePicker startPicker;
        private readonly ComboBox weekStartBox;
        private readonly NumericUpDown weeksBox;
        private readonly NumericUpDown currentWeekBox;

        public ScheduleSettingsForm(ScheduleConfig source)
        {
            Config = Clone(source);
            Text = "课表设置";
            Width = 520;
            Height = 360;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 10F);
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 7 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);
            nameBox = new TextBox { Text = Config.Name, Dock = DockStyle.Fill };
            startPicker = new DateTimePicker { Value = Config.SemesterStart, Format = DateTimePickerFormat.Short, Dock = DockStyle.Left, Width = 150 };
            weekStartBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left, Width = 150 };
            weekStartBox.Items.AddRange(new object[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" });
            weekStartBox.SelectedIndex = Math.Max(0, Math.Min(6, Config.WeekStart - 1));
            weeksBox = new NumericUpDown { Minimum = 1, Maximum = 60, Value = Config.SemesterWeeks, Width = 100 };
            currentWeekBox = new NumericUpDown { Minimum = 1, Maximum = 60, Value = Config.CurrentWeek, Width = 100 };
            AddRow(panel, 0, "课表名称", nameBox);
            AddRow(panel, 1, "开学日期", startPicker);
            AddRow(panel, 2, "每周起始日", weekStartBox);
            AddRow(panel, 3, "学期周数", weeksBox);
            AddRow(panel, 4, "当前周", currentWeekBox);
            var hint = new Label { Text = "课程周次会按开学日期和每周起始日换算。", ForeColor = Color.Gray, AutoSize = true };
            panel.Controls.Add(hint, 0, 5); panel.SetColumnSpan(hint, 2);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            var save = new Button { Text = "保存", Width = 90, DialogResult = DialogResult.OK };
            save.Click += (s, e) => { if (!Apply()) DialogResult = DialogResult.None; };
            var cancel = new Button { Text = "取消", Width = 90, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(save); buttons.Controls.Add(cancel);
            panel.Controls.Add(buttons, 0, 6); panel.SetColumnSpan(buttons, 2);
            AcceptButton = save; CancelButton = cancel;
        }

        private void AddRow(TableLayoutPanel panel, int row, string label, Control control)
        {
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
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
