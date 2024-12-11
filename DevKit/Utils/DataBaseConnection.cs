using System;
using DevKit.Cache;
using SQLite;

namespace DevKit.Utils
{
    public class DataBaseConnection : SQLiteConnection
    {
        public DataBaseConnection() : base($@"{AppDomain.CurrentDomain.BaseDirectory}DevKit.db")
        {
            CreateTable<ApkConfigCache>();
            CreateTable<ClientConfigCache>();
            CreateTable<ClientMessageCache>();
            CreateTable<ExCommandCache>();
            CreateTable<ColorResourceCache>();
        }
    }
}