#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public static class PersistentDataUtil
    {
        public static NotNullFile<T> CreateJsonFile<T>(string filename)
            where T : new()
        {
            var io = new FileIO(
                resolver: new Files.FileResolver(Application.persistentDataPath),
                serializeResolver: new Files.JsonResolver());
            return new NotNullFile<T>(filename, io);
        }
        public static NotNullFile<T> CreateEncryptedFile<T>(string filename, string password, byte[] salt, int iterations)
            where T : new()
        {
            var io = new FileIO(
                resolver: new Files.FileResolver(Application.persistentDataPath),
                serializeResolver: new Files.JsonResolver(password, salt, iterations));
            return new NotNullFile<T>(filename, io);
        }

        public static NullableFile<T> CreateNullableEncryptedFile<T>(string filename, string password, byte[] salt, int iterations)
        {
            var io = new FileIO(
                resolver: new Files.FileResolver(Application.persistentDataPath),
                serializeResolver: new Files.JsonResolver(password, salt, iterations));
            return new NullableFile<T>(filename, io);
        }
    }
}
