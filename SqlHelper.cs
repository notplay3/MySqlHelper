using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using System.Data;

namespace ConsoleApp1
{

    internal class SqlHelper
    {

        //状态码
        static private string[] StateCode =
        {
            "函数调用异常",
            "连接成功",
            "连接字段异常",
            "没找到数据库",
            "4",
            "5",
            "6",
        };

        //定义一个字段,set,get使用的变量是属性名，而不是字段名所以需要额外定义字段名
        static private string _state = null;

        //创建数据库连接对象
        static private SQLiteConnection sqliteConnection = new SQLiteConnection();
        static private SQLiteCommand cmd = new SQLiteCommand();
        /// <summary>
        /// 返回状态码对应解释
        /// </summary>
        static public string State
        {
            get { return _state; }
            private set { _state = value; }
        }

        /// <summary>
        /// 数据库连接字段
        /// </summary>
        static public string connectionString = null;


        /// <summary>
        /// 连接数据库，没有不创建，官方加密需要收费，微软加密不支持.net Framework 4.6.2,----瓜娃子
        /// </summary>
        /// <returns>返回状态码，用State属性可以获取</returns>
        /// <exception cref="Exception"></exception>
        static public int Connection()
        {
            try
            {
                SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder(@connectionString);//创建连接字段
                builder.Version = 3;//默认版本
                //判断连接字段是否正常
                if (builder.DataSource == null)
                {
                    State = StateCode[2];
                    return 2;
                }
                //判断路径是否正确
                if (!File.Exists(builder.DataSource))
                {
                    //SQLiteConnection.CreateFile(builder.DataSource);
                    State = StateCode[3];
                    return 3;//没找到数据库
                }


                sqliteConnection.ConnectionString = builder.ConnectionString;
                sqliteConnection.Open();

                State = StateCode[1];

                return 1;//连接成功
            }
            catch (Exception e)
            {
                State = StateCode[0];
                throw new Exception(e.Message);//有错误则抛出异常
            }

        }


        /// <summary>
        /// 创建数据库表
        /// </summary>
        /// <param name="command">输入表字段  表明(字段 数据类型,....）</param>
        /// <returns>1 成功</returns>
        /// <exception cref="Exception"></exception>
        static public int CreateTable(string command)
        {
            try
            {
                //判断数据库连接            
                if (Connection() != 1)
                {
                    return (Connection());
                }



                //将命令对象与连接对象联系起来
                cmd.Connection = sqliteConnection;
                //复制sql语句
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS " + command;
                //执行sql语句
                cmd.ExecuteNonQuery();

                //关闭数据库
                sqliteConnection.Close();

                return 1;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }

        }


        /// <summary>
        /// 删除表
        /// </summary>
        /// <param name="command">输入表明</param>
        /// <returns>1 成功</returns>
        /// <exception cref="Exception"></exception>
        static public int DeleteTable(string command)
        {
            //判断数据库连接                      
            if (Connection() != 1)
            {
                return (Connection());
            }
            try
            {

                //将命令对象与连接对象联系起来
                cmd.Connection = sqliteConnection;
                //复制sql语句
                cmd.CommandText = "DROP TABLE " + command;
                //执行sql语句
                cmd.ExecuteNonQuery();

                //关闭数据库
                sqliteConnection.Close();
                //sqliteConnection = null;
                return 1;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }

        }


        /// <summary>
        /// 更改表名
        /// </summary>
        /// <param name="name1">原表名</param>
        /// <param name="name2">更改后表名</param>
        /// <returns>1 成功</returns>
        /// <exception cref="Exception"></exception>
        static public int UpTableNmae(string name1, string name2)
        {
            //判断数据库连接                      
            if (Connection() != 1)
            {
                return (Connection());
            }
            try
            {

                //将命令对象与连接对象联系起来
                cmd.Connection = sqliteConnection;
                //复制sql语句
                cmd.CommandText = $"ALTER TABLE {name1} RENAME TO {name2}";
                //执行sql语句
                cmd.ExecuteNonQuery();

                //关闭数据库
                sqliteConnection.Close();

                return 1;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }

        }





        /// <summary>
        /// 更改表字段,暂不支持更改字段数据类型
        /// </summary>
        /// <param name="oldtable">旧表名</param>
        /// <param name="newtable">新表名</param>
        /// <param name="field">[新字段,数据类型,新字段,数据类型]的数组</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static public int SetTable(string oldtable, string newtable, params string[] field)
        {
            try
            {
                if (Connection() != 1)
                {
                    return Connection();
                }


                cmd.Connection = sqliteConnection;
                //赋值sql命令 查询名字为tableName的建表数据结构
                cmd.CommandText = $"SELECT sql FROM sqlite_master WHERE type='table' AND tbl_name='{oldtable}'";
                //读tableName的建表数据结构
                SQLiteDataReader sr = cmd.ExecuteReader();
                sr.Read();
                string oldcommand = sr.GetString(0);
                sr.Close();

                //创建新表sql语句
                string newcommand = "CREATE TABLE temptable(";
                for (int i = 0; i < field.Length; i += 2)
                {
                    newcommand += $"{field[i]} {field[i + 1]},";
                }
                newcommand = newcommand.Substring(0, newcommand.Length - 1) + ")";

                //return newcommand;
                //创建新的表
                cmd.CommandText = newcommand;
                cmd.ExecuteNonQuery();
                //构造复制sql语句
                string copysql = "INSERT INTO temptable SELECT ";
                for (int i = 0; i < field.Length; i += 2)
                {
                    copysql += $"\"{field[i]}\",";
                }
                copysql = copysql.Substring(0, copysql.Length - 1) + $" FROM {oldtable}";



                //将旧表的数据拷贝到新表
                using (SQLiteTransaction tr = sqliteConnection.BeginTransaction())
                {
                    cmd.CommandText = copysql;
                    cmd.ExecuteNonQuery();
                    tr.Commit();

                }
                //删除旧表
                cmd.CommandText = $"DROP TABLE IF EXISTS {oldtable}";
                cmd.ExecuteNonQuery();

                //将零时表名改为新表名
                cmd.CommandText = $"ALTER TABLE temptable RENAME TO {newtable}";
                cmd.ExecuteNonQuery();

                return 1;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                sqliteConnection = null;
            }

        }




        /// <summary>
        /// 可以执行任何sql语句
        /// </summary>
        /// <param name="SQLsentence">需要执行的sql语句</param>
        /// <returns>1 成功</returns>
        /// <exception cref="Exception"></exception>
        static public int DataManipulation(string SQLsentence)
        {
            try
            {
                if (Connection() != 1)
                {
                    return Connection();
                }

                cmd.Connection = sqliteConnection;
                cmd.CommandText = SQLsentence;
                cmd.ExecuteNonQuery();

                return 1;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }
        }




        /// <summary>
        /// 向表里插入数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="field">字段数组，英文','隔开</param>
        /// <param name="data">数据数组，英文','隔开</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static public int InsertData(string tableName, string[] field, string[] data)
        {
            try
            {
                if (Connection() != 1)
                {
                    return Connection();
                }

                //构造sql语句
                string SQLsentence = $"INSERT INTO {tableName} ";
                if (field != null)
                {
                    SQLsentence += "(";
                    for (int i = 0; i < field.Length; i++)
                    {
                        SQLsentence += $"{field[i]},";
                    }
                    SQLsentence = SQLsentence.Substring(0, SQLsentence.Length - 1);
                    SQLsentence += ")";
                }
                SQLsentence += " VALUES (";
                for (int i = 0; i < data.Length; i++)
                {
                    SQLsentence += $"{data[i]},";
                }
                SQLsentence = SQLsentence.Substring(0, SQLsentence.Length - 1);
                SQLsentence += ")";

                //return SQLsentence;



                //创建执行sql实列

                cmd.Connection = sqliteConnection;

                cmd.CommandText = $"SELECT * FROM sqlite_master WHERE type = 'table' AND tbl_name = '{tableName}'";
                SQLiteDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (reader.GetValue(1).ToString() != tableName)
                {
                    return 2;
                }
                reader.Close();

                cmd.CommandText = SQLsentence;
                //判断返回的受影响数是否等一1
                CommandBehavior behavior = new CommandBehavior();
                if (cmd.ExecuteNonQuery(behavior) == 1)
                {
                    return 1;
                }
                return 0;


            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }
        }



        /// <summary>
        /// 根据一个条件删除数据，多个条件删除多个对应的值，执行如下语句
        /// SQLsentence = $"DELETE FROM {nameTable} WHERE {字段}={值]}"
        /// </summary>
        /// <param name="nameTable">表名</param>
        /// <param name="value">[字段,条件值,....]</param>
        /// <returns>1 成功,抛出错误</returns>
        /// <exception cref="Exception"></exception>
        static public int DeleteData(string nameTable, params string[] value)
        {
            //定义sql语句变量
            string SQLsentence = null;


            if (Connection() != 1)
            {
                return Connection();
            }
            try
            {
                //构建sqly语句,删除每一个条件的数据
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i += 2)
                    {
                        SQLsentence = $"DELETE FROM {nameTable} WHERE {value[i]}={value[i + 1]}";
                        cmd.CommandText = SQLsentence;
                        cmd.Connection = sqliteConnection;
                        if (cmd.ExecuteNonQuery() != 1)
                        {
                            return 0;
                        }


                    }
                    return 1;
                }
                else//删除全表执行这个
                {
                    SQLsentence = $"DELETE FROM {nameTable}";
                    cmd.CommandText = SQLsentence;
                    cmd.Connection = sqliteConnection;
                    cmd.ExecuteNonQuery();
                    return 1;
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }


        }


        /// <summary>
        /// 更改数据内容，只支持单个条件，多个数据
        /// </summary>
        /// <param name="nameTable">表名</param>
        /// <param name="condition">条件，ex:"ID=8"</param>
        /// <param name="keyValue">[字段,新值,....]</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static public int UpData(string nameTable, string condition, params string[] keyValue)
        {
            int state = Connection();
            if (state != 1)
            {
                return state;
            }
            try
            {
                //构建sql
                //string SQLsentence;
                cmd.Connection = sqliteConnection;
                for (int i = 0; i < keyValue.Length; i += 4)
                {
                    cmd.CommandText = $"UPDATE {nameTable} SET {keyValue[i]}={keyValue[i + 1]} WHERE {condition}";
                    if (cmd.ExecuteNonQuery() != 1)
                    {
                        return 0;
                    }
                }
                return 1;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                sqliteConnection = null;
            }
        }




        /// <summary>
        /// 获取数据，单个条件，多个数据，或者全部数据
        /// </summary>
        /// <param name="nameTable">表名</param>
        /// <param name="keyValue">[字段,条件值,.....]</param>
        /// <returns>嵌套数组</returns>
        static public string[][] SelectData(string nameTable, params string[] keyValue)
        {
            try
            {
                //连接数据库并获取返回值
                int state = Connection();
                cmd.Connection = sqliteConnection;
                SQLiteDataReader reader;
                //返回的嵌套数组
                string[][] data = null;
                //单挑数据临时数组
                cmd.CommandText = $"SELECT * from {nameTable}";
                reader = cmd.ExecuteReader();
                string[] temp = new string[reader.GetValues().Count];
                reader.Close();

                if (state != 1)
                {
                    data[0] = new string[] { state.ToString() };
                    return data;
                }


                //如果条件为空返回
                if (keyValue == null)
                {
                    data[0] = new string[] { "0" };
                    return data;

                }



                if (keyValue[0] == "*")
                {
                    //构建查询表数据总数的sql，定义data的长度
                    cmd.CommandText = $"SELECT count(*) from {nameTable}";
                    int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
                    data = new string[rowCount][];



                    //构建查询所以数据的sql
                    cmd.CommandText = $"SELECT * FROM {nameTable}";
                    reader = cmd.ExecuteReader();

                    int i = 0;
                    //循环读取数据，将数据存入temp中
                    while (reader.Read())
                    {
                        for (int j = 0; j < reader.GetValues().Count; j++)
                        {
                            if (reader.GetFieldType(j) == typeof(System.Int64))
                            {
                                temp[j] = reader.GetFloat(j).ToString();
                            }
                            else
                            {
                                temp[j] = reader.GetValue(j).ToString();
                            }
                        }
                        //
                        data[i] = temp;
                        i++;
                    }
                    reader.Close();
                    return data;

                }
                else
                {
                    data = new string[keyValue.Length / 2][];

                    //构建sql,单个条件多个数据执行
                    for (int i = 0; i < keyValue.Length; i += 2)
                    {
                        cmd.CommandText = $"SELECT * FROM {nameTable} WHERE {keyValue[i]}={keyValue[i + 1]}";
                        reader = cmd.ExecuteReader();
                        reader.Read();
                        for (int j = 0; j < reader.GetValues().Count; j++)
                        {
                            if (reader.GetFieldType(j) == typeof(System.Int64))
                            {
                                temp[j] = reader.GetFloat(j).ToString();
                            }
                            else
                            {
                                temp[j] = reader.GetValue(j).ToString();
                            }
                        }
                        data[i] = temp;
                        reader.Close();
                    }

                    return data;
                }
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }


        }


    }
}
