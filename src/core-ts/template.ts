import * as XLSX from 'xlsx';

export function writeTemplate(): Uint8Array {
  const workbook = XLSX.utils.book_new();
  const data = [
    ['星期', '课程名称', '教师', '周次', '教室', '节次', '备注'],
    [1, '高等数学', '王老师', '1-16', '教学楼A101', '01-02', '示例行，可删除'],
    [3, '大学英语', '李老师', '1-8', '教学楼B203', '05-06', '示例行，可删除'],
    ['', '', '', '', '', '', ''],
    ['', '', '', '', '', '', ''],
  ];
  const dataSheet = XLSX.utils.aoa_to_sheet(data);
  dataSheet['!cols'] = [{ wch: 10 }, { wch: 24 }, { wch: 16 }, { wch: 14 }, { wch: 18 }, { wch: 12 }, { wch: 24 }];
  dataSheet['!autofilter'] = { ref: 'A1:G5' };
  dataSheet['!freeze'] = { xSplit: 0, ySplit: 1 };
  XLSX.utils.book_append_sheet(workbook, dataSheet, '课程数据');

  const helpSheet = XLSX.utils.aoa_to_sheet([
    ['课程表模板使用说明'],
    ['请在“课程数据”工作表中编辑，每一行代表一门课程。'],
    ['星期：填写 1-7，分别代表周一至周日。'],
    ['周次：支持 1-16、1,3,5 等写法。'],
    ['节次：支持 01-02、5-6 等写法。'],
    ['导入时会忽略空行和无法识别的记录。'],
  ]);
  helpSheet['!cols'] = [{ wch: 64 }];
  XLSX.utils.book_append_sheet(workbook, helpSheet, '使用说明');
  return XLSX.write(workbook, { bookType: 'xlsx', type: 'array' }) as Uint8Array;
}
