﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DDD.BuildingBlocks.Core.Util
{
    public static class SerializerHelper
    {
        private static void SaveToJson(string strFile, List<object> objects)
        {
            var serializerSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var content = JsonConvert.SerializeObject(objects, serializerSetting);

            File.WriteAllText(strFile, content);
        }

        private static List<object>? LoadFromJson(string strFile)
        {
            var serializerSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var content = File.ReadAllText(strFile);
            var obj = JsonConvert.DeserializeObject<List<Object>>(content, serializerSetting);
            return obj;
        }

        public static void SaveListToFile<T>(string file, IEnumerable<T> items)
        {
            var objects = items.Select(o => (object)o!).ToList();
            SaveToJson(file, objects!);
        }

        public static List<T> LoadListFromFile<T>(string file)
        {
            var results = LoadFromJson(file);

            return results?.Select(o => (T)o).ToList() ?? throw new InvalidOperationException();
        }
    }
}
