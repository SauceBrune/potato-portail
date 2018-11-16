﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace SysInternshipManagement.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [StringLength(50)]
        [Display(Name = "Nom")]
        public string nom { get; set; }

        [StringLength(50)]
        [Display(Name = "Prénom")]
        public string prenom { get; set; }

        //Les rôles doivent être donnés au model par AccountController ou ManageController afin de pouvoir être utilisé.
        public IEnumerable<string> roles { get; set; }

        //Cette propriété effectue une concaténation de tous les rôles qui se trouve dans "roles".
        [Display(Name = "Rôles")]
        public string rolesWrap
        {
            get
            {
                string line = "";
                foreach(string role in roles)
                {
                    line += role + ", ";
                }
                if(line != "")
                    return line.Remove(line.Length - 2, 2);
                return "Error";
            }
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Notez qu'authenticationType doit correspondre à l'élément défini dans CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Ajouter les revendications personnalisées de l’utilisateur ici
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("BDPlanCadre", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}