using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace MES_LINK_VSKY.models.GlueMachine
{
    public struct MyTools
    {
        public string Keys { set; get; }

        public string Values { set; get; }

    }

    public class StringToJson
    {
        private static StringToJson _StrToJson = null;

        public static StringToJson SingletonInstance
        {
            get
            {
                if (_StrToJson == null)
                {
                    _StrToJson = new StringToJson();
                }
                return _StrToJson;
            }
        }

        /// <summary>
        /// 输出json数组,可以嵌套
        /// </summary>
        //public void StringToJsonArray(List<object> tools)
        //{
        //    try
        //    {
        //        //List<object> list_son = new List<object>();
        //        //for (int i = 0; i < tools.Count; i++)
        //        //{
        //        //    if (StrToJsonArray(((MyTools)tools[i]).Keys, ((MyTools)tools[i]).Values) == null)
        //        //    {
        //        //        state = State_StrToJson.Aberrant;
        //        //        break;
        //        //    }
        //        //    list_son.Add(StrToJsonArray(((MyTools)tools[i]).Keys, ((MyTools)tools[i]).Values));
        //        //}
        //        //if (state == State_StrToJson.IsOk)
        //        //{
        //        //    _result = JsonConvert.SerializeObject(list_son);
        //        //}
        //    }
        //    catch (Exception)
        //    {
        //        //state = State_StrToJson.Aberrant;
        //        //return;
        //    }
        //}


        /// <summary>
        /// 输出一串json,可以嵌套
        /// 输入一个list容器，容器中每一个element的格式为：key,value1 这种string类型，否则会出现异常
        /// </summary>
        public string StringToJsonText(List<string> source)
        {
            try
            {               
                Dictionary<string, object> temporary_Dic = new Dictionary<string, object>();

                for (int i = 0; i < source.Count; i++)
                {
                    if (source[i].Split(';').Length == 2)
                    {
                        temporary_Dic.Add(source[i].Split(';')[0], source[i].Split(';')[1]);
                    }
                    else if(source[i].Split(';').Length == 3)
                    {
                        MyTools temp_tool = new MyTools
                        {
                            Keys = source[i].Split(';')[1],
                            Values = source[i].Split(';')[2]
                        };
                        temporary_Dic.Add(source[i].Split(';')[0], temp_tool);
                    }
                    else
                    {
                        return null;
                    }
                }
                return JsonConvert.SerializeObject(temporary_Dic);

            }
            catch (Exception)
            {
                return null;
            }
        }
        public void SaveCSV(DataTable dt, string fullPath)
        {
            var fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);            
            var sw = new StreamWriter(fs, Encoding.UTF8);
            var data = "";

            //写出列名称           
            for (var i = 0; i < dt.Columns.Count; i++)
            {
                data += dt.Columns[i].ColumnName;
                if (i < dt.Columns.Count - 1)
                {
                    data += ",";
                }
            }
            sw.WriteLine(data);
            //写出各行数据           
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                data = "";

                for (var j = 0; j < dt.Columns.Count; j++)
                {
                    var str = dt.Rows[i][j].ToString();
                    str = str.Replace("\"", "\"\""); //替换英文冒号 英文冒号需要换成两个冒号     
                    if (str.Contains(',') || str.Contains('"') || str.Contains('\r') || str.Contains('\n')) //含逗号 冒号 换行符的需要放到引号中   
                    {
                        str = string.Format("\"{0}\"", str);
                    }
                    data += str;
                    if (j < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }
                }
                sw.WriteLine(data);
            }
            sw.Close();
            fs.Close();
        }

    }
}
