using ExcelDataReader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ExcelTool
{
    /// <summary>
    /// excel文件存放的路径
    /// </summary>
    public static string EXCEL_PATH = Application.dataPath + "/ArtRes/Excel/";

    public static string DATA_CLASS_PATH = Application.dataPath + "/Scripts/ExcelData/DataClass/";
    /// <summary>
    /// 容器类脚本存储位置
    /// </summary>
    public static string DATA_CONTAINER_PATH = Application.dataPath + "/Scripts/ExcelData/Container/";



    public static int BEGIN_INDEX = 4;

    [MenuItem("GameTool/GenerateExcel")]
    private static void GenerateExcelInfo()
    {
        //记载指定路径中的所有Excel文件 用于生成对应的3个文件
        DirectoryInfo dInfo = Directory.CreateDirectory(EXCEL_PATH);
        //得到指定路径中的所有文件信息 相当于就是得到所有的Excel表
        FileInfo[] files = dInfo.GetFiles();
        DataTableCollection tableConllection;
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Extension != ".xlsx" && files[i].Extension != ".xls")
            {
                continue;
            }
            
            using(FileStream fs = files[i].Open(FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(fs);
                tableConllection = excelReader.AsDataSet().Tables;
                fs.Close();
            }

            foreach (DataTable table in tableConllection)
            {
                Debug.Log(table.TableName);
                //生成数据结构类
                GenerateExcelDataClass(table);

                //生成容器类
                GenerateExcelContainer(table);

                //生成2进制数据
                GenerateExcelBinary(table);
            }
        }
    }

    /// <summary>
    /// 生成Excel表对应的数据结构类
    /// </summary>
    /// <param name="table"></param>
    private static void GenerateExcelDataClass(DataTable table)
    {
        DataRow rowName = GetVariableNameRow(table);
        DataRow rowType = GetVariableTypeRow(table);

        if (!Directory.Exists(DATA_CLASS_PATH))
        {
            Directory.CreateDirectory(DATA_CLASS_PATH);
        }
        //如果我们要生成对应的数据结构类脚本 其实就是通过代码进行字符串拼接 然后存进文件就行了
        string str = "public class " + table.TableName + "\n{\n";

        for (int i = 0; i < table.Columns.Count; i++)
        {
            str += "    public " + rowType[i].ToString() + " " + rowName[i].ToString() + ";\n";
        }

        str += "}";
        //把拼接好的字符串 写入指定文件中
        File.WriteAllText(DATA_CLASS_PATH + table.TableName + ".cs", str);

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 生成Excel表对应的数据容器类（Dictionary）
    /// </summary>
    /// <param name="table"></param>
    private static void GenerateExcelContainer(DataTable table)
    {
        //获取主键索引
        int keyIndex = GetKeyIndex(table);
        //得到字段类型行
        DataRow rowType = GetVariableTypeRow(table);

        if (!Directory.Exists(DATA_CONTAINER_PATH))
        {
            Directory.CreateDirectory(DATA_CONTAINER_PATH);
        }

        string str = "using System.Collections.Generic;\n";
        str += "public class " + table.TableName + "Container" + "\n{\n";

        str += "    ";

        str += "public Dictionary<" + rowType[keyIndex].ToString() + ", " + table.TableName + "> dataDic = new Dictionary<" + rowType[keyIndex].ToString() + ", " + table.TableName + ">();\n";

        str += "}";

        File.WriteAllText(DATA_CONTAINER_PATH + table.TableName + "container.cs", str);

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 生成二进制数据
    /// </summary>
    /// <param name="table"></param>
    private static void GenerateExcelBinary(DataTable table)
    {
        if (!Directory.Exists(BinaryDataMgr.DATA_BINARY_PATH))
        {
            Directory.CreateDirectory(BinaryDataMgr.DATA_BINARY_PATH);
        }

        //创建一个2进制文件进行写入
        using(FileStream fs = new FileStream(BinaryDataMgr.DATA_BINARY_PATH + table.TableName + ".tang", FileMode.OpenOrCreate, FileAccess.Write))
        {
            //存储具体的excel对应的2进制信息
            //1.先要存储我们需要写多少行数据 方便我们读取
            //-4的原因 是因为前面4行是配置规则 并不是我们需要记录的数据内容 
            fs.Write(BitConverter.GetBytes(table.Rows.Count - 4), 0, 4);
            //2.存储主键的变量名 (id)
            string keyName = GetVariableNameRow(table)[GetKeyIndex(table)].ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(keyName);
            //存储字符串字节数组的长度
            fs.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
            fs.Write(bytes, 0, bytes.Length);

            DataRow row;
            //得到类型行 根据类型来决定应该如何写入数据
            DataRow rowType = GetVariableTypeRow(table);
            for (int i = BEGIN_INDEX; i < table.Rows.Count; i++)
            {
                row = table.Rows[i];
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    switch (rowType[j].ToString())
                    {
                        case "int":
                            fs.Write(BitConverter.GetBytes(int.Parse(row[j].ToString())), 0, 4);
                            break;
                        case "float":
                            fs.Write(BitConverter.GetBytes(float.Parse(row[j].ToString())), 0, 4);
                            break;
                        case "bool":
                            fs.Write(BitConverter.GetBytes(bool.Parse(row[j].ToString())), 0, 1);
                            break;
                        case "string":
                            bytes = Encoding.UTF8.GetBytes(row[j].ToString());
                            //写入字符串长度
                            fs.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
                            //写入字符串本身
                            fs.Write(bytes, 0, bytes.Length);
                            break;
                    }
                }
            }

            fs.Close();
        }

        AssetDatabase.Refresh();
    }

    public static DataRow GetVariableNameRow(DataTable table)
    {
        return table.Rows[0];
    }

    public static DataRow GetVariableTypeRow(DataTable table)
    {
        return table.Rows[1];
    }

    /// <summary>
    /// 获取主键Key索引 即获取Excel表中key在哪一列
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public static int GetKeyIndex(DataTable table)
    {
        //key固定在第三行
        DataRow row = table.Rows[2];
        for (int i = 0; i < table.Columns.Count; i++)
        {
            if (row[i].ToString() == "key")
            {
                return i;
            }
        }
        return 0;
    }
}
