using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFR
{
    public class Person
    {
        public string Name { get; set; }
        public int ID { get; set; }
    
        public Person(string name, int id)
        {
            this.Name = name;
            this.ID = id;
        }
        public static string personsToJson(List<Person> persons)
        {
            return JsonConvert.SerializeObject(persons);
        }   
        
        public static List<Person> personsFromJson(string json)
        {
           return  JsonConvert.DeserializeObject<List<Person>>(json);
        }
        public static int findPersonID(List<Person> people,string name)
        {
            foreach (var item in people)
            {
                if (item.Name == name)
                    return item.ID;
            }
            return -1;
        }
        public static int setID(List<Person> people )
        {
            if (people.Count == 0)
                return 1;

            int newID = 1;
            foreach (var item in people)
            {
                if (item.ID > newID)
                    newID = item.ID;
            }

            return (newID + 1);
        }
        public static string findNameByID(List<Person> people,int ID)
        {
            foreach (var item in people)
            {
                if (ID == item.ID)
                    return item.Name;
            }
            return "";
        }
    }
}
