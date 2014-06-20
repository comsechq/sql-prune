using System;
using System.Collections.Generic;
using System.Reflection;
using Moq;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Base class designed to ease testing with Mock.
    /// </summary>
    public abstract class AutoMockingTest
    {
        private readonly IDictionary<Type, Object> mocks = new Dictionary<Type, object>();

        protected Mock<T> Mock<T>() where T : class
        {
            var type = typeof(T);

            return (Mock<T>)mocks[type];
        }

        /// <summary>
        /// Creates an object of the specified type.
        /// </summary>
        /// <typeparam name="T">A type to create.</typeparam>
        /// <returns>
        /// Object of the type <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>Usually used to create objects to test. Any non-existing dependencies
        /// are mocked.
        /// <para>Container is used to resolve build dependencies.</para></remarks>
        protected T Create<T>() where T : class, new()
        {
            var result = new T();

            mocks.Clear();
            
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            foreach (var info in properties)
            {
                if (
                    info.PropertyType.Name.StartsWith("I") &&
                    (
                        info.PropertyType.Name.EndsWith("Service") |
                        info.PropertyType.Name.EndsWith("Repository") |
                        info.PropertyType.Name.EndsWith("Scraper") |
                        info.PropertyType.Name.EndsWith("Validator") |
                        info.PropertyType.Name.EndsWith("Loader") |
                        info.PropertyType.Name.EndsWith("Bank") |
                        info.PropertyType.Name.EndsWith("Extractor") |
                        info.PropertyType.Name.EndsWith("Strategy")
                    )
                )
                {
                    if (!mocks.ContainsKey(info.PropertyType))
                    {
                        var mock = Activator.CreateInstance(typeof(Mock<>).MakeGenericType(info.PropertyType));

                        var obj = mock.GetType().GetProperty("Object", info.PropertyType).GetValue(mock, null);

                        info.SetValue(result, obj, null);

                        mocks.Add(info.PropertyType, mock);
                    }
                }
            }

            return result;
        }
    }
}
