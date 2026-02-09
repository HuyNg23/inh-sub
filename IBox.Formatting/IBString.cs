using IBox.Common.Objects;
using Newtonsoft.Json;
using System.Collections;
using System.Security.Cryptography;

namespace IBox.Formatting
{
    public class IBString : IString
    {
        public string AddEnd(string source, string material)
        {
            return source + material;
        }

        public string Replace(string source, string replateString, string material)
        {
            return source.Replace(replateString, material);
        }

        public dynamic SplitToObject(string source, char character, string formatting, Type type, string tenantId)
        {
            if (string.IsNullOrEmpty(formatting))
            {
                throw new IboxLog("formatting model must have temp value", tenantId);
            }

            if (string.IsNullOrEmpty(character.ToString()))
            {
                throw new IboxLog("character split must have temp value", tenantId);
            }

            if (type == null)
            {
                throw new IboxLog("type format for split must be defined", tenantId);
            }

            foreach (var item in source.Split(character))
            {
                formatting = string.Format(formatting, item);
            }

            return JsonConvert.DeserializeObject(formatting, type) ?? new object() { };
        }

        public dynamic ListToObject(IList values, string formatting, Type type, string tenantId)
        {
            if (string.IsNullOrEmpty(formatting))
            {
                throw new IboxLog("formatting model must have temp value", tenantId);
            }

            if (type == null)
            {
                throw new IboxLog("type format for split must be defined", tenantId);
            }


            var lst = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));


            if (lst == null)
            {
                throw new IboxLog(string.Format("Can not Create Instance of list Generic type {0}", type.Name), tenantId);
            }

            foreach (var item in values)
            {
                lst.Add(JsonConvert.DeserializeObject(string.Format(formatting, item), type));
            }

            return lst;
        }

        public string GetGUID()
        {
            return Guid.NewGuid().ToString();
        }

        public string RandomNumber()
        {
            List<int> listNumber = Enumerable.Range(0, 10).ToList();
            byte[] randomBytes = new byte[1];

            int n = listNumber.Count;
            while (n > 1)
            {
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }

                int k = randomBytes[0] % n;
                n--;

                int temp = listNumber[n];
                listNumber[n] = listNumber[k];
                listNumber[k] = temp;
            }

            return string.Join("", listNumber);
        }

        public string GetDateTime(string FomatDate)
        {
            return DateTime.Now.ToString(FomatDate);
        }
    }
}