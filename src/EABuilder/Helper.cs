using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EABuilder
{
    public static class Helper
    {
        public static IEnumerable<string> CompileExpressiveAttributes(this Assembly assembly)
        {
            var eadll = assembly.GetReferencedAssemblies().FirstOrDefault(x => x.Name == "ExpressiveAnnotations");
            if (eadll == null)
                return new string[0];

            return assembly.GetTypes().SelectMany(t => t.CompileExpressiveAttributes());
        }

        public static IEnumerable<string> CompileExpressiveAttributes(this Type modelType)
        {
            var errors = new List<Exception>();

            var properties = modelType.GetProperties().ToList();
            properties.ForEach(prop =>
            {
                var expressiveAttribs = prop.GetCustomAttributes()
                    .Where(a => a.GetType().BaseType?.Name == "ExpressiveAttribute").ToList();
                expressiveAttribs.ForEach(attrib =>
                {
                    try
                    {
                        var dynamicAttrib = (dynamic) attrib;
                        dynamicAttrib.Compile(prop.DeclaringType);
                    }
                    catch (Exception e)
                    {
                        if (e.Source == "ExpressiveAnnotations")
                            errors.Add(e);
                    }
                });
            });

            return errors.Select(x => x.Message);
        }
    }
}