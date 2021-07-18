using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ToreAurstadIT.DapperUtils
{
    /// <summary>
    /// Contains reflection helpers for this Dapper Extensions lib. Keep modifiers here public and not internal in case tests are to be added and
    /// C# lacks convenient friend assembly notation.
    /// </summary>
    public static class ReflectionHelper
    {

        /// <summary>
        /// Returns the properties and their column names for a given <typeparamref name="TTable"/> table via public properties.
        /// If a property is marked with [NotMapped] attribute, it is skipped if not the <paramref name="includePropertiesMarkedAsNotMapped"/>
        /// is set to to true. If a property is marked with [Key] attribute and if the [DatabaseGeneratedOptions] attribute exists, it is not equal to 
        /// DatabaseGeneratedOptions.None, it is skipped if the <paramref name="includePropertiesMarkedAsKeyOrNotDatabaseGenerated"/> is set to true.
        /// </summary>
        /// <typeparam name="TTable"></typeparam>
        /// <param name="includePropertiesMarkedAsKeyOrNotDatabaseGenerated">Defaults to true. To get properties for inserts, it is adviced to pass true here.</param>
        /// <param name="includePropertiesMarkedAsNotMapped">Defaults to false. To get properties marked as [NotMapped], pass true here (not recommended).</param>
        /// <returns></returns>
        public static Dictionary<string, PropertyInfo> GetPublicProperties<TTable>(bool includePropertiesMarkedAsKeyOrNotDatabaseGenerated = true, 
            bool includePropertiesMarkedAsNotMapped = false)
        {
            var props = typeof(TTable).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            props = props.Where(p => !HasAttributeOfType<NotMappedAttribute>(p) || includePropertiesMarkedAsNotMapped).ToArray();
            props = props.Where(p => ((!HasAttributeOfType<KeyAttribute>(p) 
            && !HasAttributeOfTypeEqualing<DatabaseGeneratedAttribute>(p, DatabaseGeneratedOption.Computed)
            && !HasAttributeOfTypeEqualing<DatabaseGeneratedAttribute>(p, DatabaseGeneratedOption.Identity))
                || includePropertiesMarkedAsKeyOrNotDatabaseGenerated)).ToArray(); 
            return props.Select(p => p)
                .ToDictionary(p => GetColumnName(p), p => p);               
        }

        private static bool HasAttributeOfTypeEqualing<T>(PropertyInfo p, DatabaseGeneratedOption dbOption)
        {
            var databaseGenerated = p.GetCustomAttribute<DatabaseGeneratedAttribute>();
            return databaseGenerated != null && databaseGenerated.DatabaseGeneratedOption == dbOption;
        }

        private static string GetColumnName(PropertyInfo p) => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;        

        private static bool HasAttributeOfType<T>(PropertyInfo p)
        {
            return p.GetCustomAttribute<NotMappedAttribute>(true) != null;
        }
    }
}
