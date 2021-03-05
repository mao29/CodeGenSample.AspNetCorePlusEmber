﻿/*
 * Этот файл генерируется каждый раз при генерации проекта объектов.
 * Не нужно вносить изменения в этот файл вручную.
 * Настроить поведение тестов можно в соседнем файле,
 * который генерируется только при первой генерации.
 */

namespace NewPlatform.SuperSimpleContactList
{
    using ICSSoft.STORMNET;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public partial class DataObjectFacts
    {
        #region Customizations
        private partial Type GetDataServiceType();

        private partial Dictionary<Type, string[]> GetPropertyWithoutDataServiceExpression();
        
        private partial Dictionary<Type, string[]> GetPropertyWithoutNotNull();
        #endregion

        private IEnumerable<Type> GetObjects()
        {
            return Assembly.GetAssembly(typeof(ObjectsMarker))
                .GetExportedTypes()
                .Where(
                    x => x.IsClass
                         && x.IsSubclassOf(typeof(DataObject))
                         && Information.IsStoredType(x))
                .OrderBy(x => x.FullName);
        }


        /// <summary>
        ///     Тест проверяет, что все нехранимые свойства имеют DSE.
        /// </summary>
        [Fact]
        public void TestAllNotStoredPropertiesHaveDataServiceExpression()
        {
            // Arrange.
            var dataServiceType = GetDataServiceType();
            var assemblyClasses = GetObjects();
            var dontCheckDict = GetPropertyWithoutDataServiceExpression();
            var dontCheckPropertyNames = typeof(DataObject).GetProperties()
                .Where(o => !Information.IsStoredProperty(typeof(DataObject), o.Name))
                .Select(o => o.Name);

            // Act.
            var errors = new List<string>();
            foreach (var cl in assemblyClasses)
            {
                var classProperties = cl.GetProperties()
                    .Where(// Не проверяем исключения на имя свойства.
                        o => !dontCheckPropertyNames.Contains(o.Name))
                    .Where(// Не проверяем пары имя свойства + класс.
                        o => !(dontCheckDict.ContainsKey(cl) && dontCheckDict[cl].Contains(o.Name)))
                    .Where(
                        o => !Information.IsStoredProperty(cl, o.Name)
                             && string.IsNullOrEmpty(Information.GetExpressionForProperty(cl, o.Name).GetMostCompatible(dataServiceType)?.ToString())
                             && !o.PropertyType.IsSubclassOf(typeof(DataObject)));
                errors.AddRange(classProperties.Select(prop => $"{cl.FullName}.{prop.Name}"));
            }

            // Assert.
            Assert.False(
                errors.Any(),
                $"{Environment.NewLine}Следующие нехранимые свойства классов не имеют DSE:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        /// <summary>
        ///     Тест проверяет, что все логические поля имеют атрибут NotNull.
        /// </summary>
        [Fact]
        public void TestAllValueTypeHasNotNullFlag()
        {
            // Arrange.
            var assemblyClasses = GetObjects();
            var dontCheckDict = GetPropertyWithoutNotNull();

            // Act.
            var errors = new List<string>();
            foreach (var cl in assemblyClasses)
            {
                var classProperties = cl.GetProperties();
                foreach (var property in classProperties)
                {
                    var propType = Information.GetPropertyType(cl, property.Name);
                    if (!propType.IsConstructedGenericType
                        && !propType.IsEnum
                        && propType.IsValueType
                        && Information.IsStoredProperty(cl, property.Name)
                        && !Information.GetPropertyNotNull(cl, property.Name)
                        && !(dontCheckDict.ContainsKey(cl) && dontCheckDict[cl].Contains(property.Name)))
                    {
                        errors.Add($@"{cl.FullName}.{property.Name}");
                    }
                }
            }

            // Assert.
            Assert.False(
                errors.Any(),
                $"{Environment.NewLine}Следующие ValueType свойства классов не имеют флага NotNull:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}
