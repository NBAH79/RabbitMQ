// See https://aka.ms/new-console-template for more information

using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Service1.Model;


using (ApplicationContext db = new ApplicationContext())
{
    // создаем два объекта User
    User user1 = new User { Name = "Вася", Description = "просто Вася" };
    User user2 = new User { Name = "Петя", Description = null };

    Car car = new Car { Name = "машина", Description = "красная" };//, Models=new List<Model1>(){user1,user2 }  };


    db.Users.AddRange(user1, user2);
    db.Cars.Add(car);

    db.SaveChanges();
    db.ShowAll();
}

Console.WriteLine("Service1");

var factory = new ConnectionFactory { HostName = "rabbitmq", UserName = "client1", Password = "111122223333" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "hello",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");
    using (ApplicationContext db = new ApplicationContext())
    {
        var u=new User { Name = message, Description = message };
        var c=new Car { Name = message, Description = message, Users=new List<User>{u } };
        db.Append(u);
        db.Append(c);
        db.SaveChanges() ;
        db.ShowAll();
    }

};
channel.BasicConsume(queue: "hello",
                     autoAck: true,
                     consumer: consumer);

Console.WriteLine(" Press [enter]");
Console.ReadLine();

public class ApplicationContext : DbContext
{
    public DbSet<Service1.Model.User> Users { get; set; } 
    public DbSet<Service1.Model.Car> Cars { get; set; } 

    public ApplicationContext()
    {
        //Database.EnsureDeleted();
        Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=postgres;Port=5432;Database=usersdb;Username=postgres;Password=11223344;");
    }

    public void ShowAll()
    {
        // получаем объекты из бд и выводим на консоль
        ////var users = this.Models1.ToList();
        ////Console.WriteLine("Users list:");
        ////foreach (Model1 u in users)
        ////{
        ////    Console.WriteLine($"{u.Id}.{u.Name} - {u.Description}");
        ////}
    }

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<Model1>()
    //        .HasOne(p => p.Model)
    //        .WithMany(t => t.Models)
    //        .HasForeignKey(p => p.Id);
    //}
    public void Append(User model)
    {
        // добавляем их в бд
        this.Users.Add(model);
        //this.SaveChanges();
    }

    public void Append(Car model)
    {
        // добавляем их в бд
        this.Cars.Add(model);
        //this.SaveChanges();
    }
}