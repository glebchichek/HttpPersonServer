using System;
using System.Collections.Generic;
using System.Text;

namespace HttpPersonClient
{
    public record class Person
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public decimal Salary { get; set; }
    }

}
