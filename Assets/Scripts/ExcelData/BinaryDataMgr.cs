using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;



/// <summary>
/// 二进制数据管理器
/// </summary>

public class BinaryDataMgr

{
    /// <summary>
    /// 内存中已加载的 Excel 表，Key = 容器类名，Value = 容器对象
    /// </summary>
    private Dictionary<string, object> tableDic = new Dictionary<string, object>();

    public static string DATA_BINARY_PATH = Application.streamingAssetsPath + "/Binary/";
    /// <summary>
    /// 游戏存档文件的存放路径
    /// </summary>
    public static string SAVE_PATH = Application.persistentDataPath + "/Data/";

    private static BinaryDataMgr instance = new BinaryDataMgr();
    public static BinaryDataMgr Instance => instance;
    private BinaryDataMgr()
    {

    }

    public void InitData()
    {

    }

    /// <summary>
    /// 从二进制文件加载一张 Excel 表到内存 
    /// </summary>
    /// <typeparam name="T">容器类（需包含 dataDic 字典字段）</typeparam>
    /// <typeparam name="K">数据结构类（每行数据对应的类）</typeparam>
    public void LoadTable<T, K>()
    {
        //打开对应的 .tang 二进制文件 
        using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(K).Name + ".tang", FileMode.Open, FileAccess.Read))
        {
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, bytes.Length);
            fs.Close();
            //当前读取进度（字节偏移）
            int index = 0;
            //读取总行数（int = 4 字节）
            int count = BitConverter.ToInt32(bytes, index);
            index += 4;
            //读取主键字段名
            //先读长度，再读字符串内容
            int keyNameLength = BitConverter.ToInt32(bytes, index);
            index += 4;
            string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
            index += keyNameLength;
            //创建容器对象
            Type contaninerType = typeof(T);
            object contaninerObj = Activator.CreateInstance(contaninerType);
            //通过反射获取数据结构类的所有字段信息
            Type classType = typeof(K);
            FieldInfo[] infos = classType.GetFields();
            //逐行读取数据
            for (int i = 0; i < count; i++)
            {
                //实例化一行数据对象
                object dataObj = Activator.CreateInstance(classType);
                foreach (FieldInfo info in infos)
                {
                    if (info.FieldType == typeof(int))
                    {
                        info.SetValue(dataObj, BitConverter.ToInt32(bytes, index));
                        index += 4;
                    }
                    else if (info.FieldType == typeof(float))
                    {
                        info.SetValue(dataObj, BitConverter.ToSingle(bytes, index));
                        index += 4;
                    }
                    else if (info.FieldType == typeof(bool))
                    {
                        info.SetValue(dataObj, BitConverter.ToBoolean(bytes, index));
                        index += 1;
                    }
                    else if (info.FieldType == typeof(string))
                    {
                        //字符串：先读长度（4 字节），再读内容
                        int length = BitConverter.ToInt32(bytes, index);
                        index += 4;
                        info.SetValue(dataObj, Encoding.UTF8.GetString(bytes, index, length));
                        index += length;
                    }
                }
                //把这一行数据添加到容器的 dataDic 字典中
                //通过反射获取 dataDic 字典，调用 Add 方法添加
                object dicObject = contaninerType.GetField("dataDic").GetValue(contaninerObj);
                MethodInfo mInfo = dicObject.GetType().GetMethod("Add");
                object keyValue = classType.GetField(keyName).GetValue(dataObj);
                mInfo.Invoke(dicObject, new object[] { keyValue, dataObj });
            }
            //加载完成的表记录到 tableDic 中，后续通过 GetTable 获取
            tableDic.Add(typeof(T).Name, contaninerObj);
            fs.Close();
        }
    }
    /// <summary>
    /// 获取已加载的 Excel 表
    /// </summary>
    /// <typeparam name="T">容器类（需包含 dataDic 字典字段）</typeparam>
    /// <returns></returns>
    public T GetTable<T>() where T : class
    {
        string tableName = typeof(T).Name;
        if (tableDic.ContainsKey(tableName))
        {
            return tableDic[tableName] as T;
        }
        return null;
    }
    /// <summary>
    /// 将对象序列化为二进制文件（用于游戏存档）
    /// </summary>
    /// <param name="obj">要保存的对象</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    public void Save(object obj, string fileName)
    {
        //确保存档目录存在
        if (!Directory.Exists(SAVE_PATH))
            Directory.CreateDirectory(SAVE_PATH);
        Debug.Log(SAVE_PATH);
        using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".tang", FileMode.OpenOrCreate, FileAccess.Write))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, obj);
            fs.Close();
        }
    }
    /// <summary>
    /// 从二进制文件反序列化为对象（用于游戏读档）
    /// </summary>
    /// <typeparam name="T">目标对象类型</typeparam>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <returns></returns>
    public T Load<T>(string fileName) where T : class
    {
        //存档文件不存在时返回 null（新游戏）
        if (!File.Exists(SAVE_PATH + fileName + ".tang"))
            return default(T);
        T obj;
        using (FileStream fs = File.Open(SAVE_PATH + fileName + ".tang", FileMode.Open, FileAccess.Read))
        {
            BinaryFormatter bf = new BinaryFormatter();
            obj = bf.Deserialize(fs) as T;
            fs.Close();
        }
        return obj;
    }
}