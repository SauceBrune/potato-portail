﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Services;
using PotatoPortail.Models;
using PotatoPortail.Helpers;
using PotatoPortail.ViewModels;
using PotatoPortail.ViewModels.PlanCadre;
using Newtonsoft.Json;
using PotatoPortail.Migrations;
using CompetenceViewModel = PotatoPortail.ViewModels.PlanCours.CompetenceViewModel;

namespace PotatoPortail.Controllers
{
    [RcpPlanCadreAuthorize]
    public class PlanCadreController : Controller
    {
        List<SelectListItem> elements;
        CompetenceViewModel competenceViewModel;
        private BdPortail db = new BdPortail();

        public ActionResult Info(int? idPlanCadre)
        {
            if (idPlanCadre == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            PlanCadre planCadre = db.PlanCadre.Find(idPlanCadre);
            if (planCadre == null)
            {
                return HttpNotFound();
            }

            return View(planCadre);
        }

        public ActionResult InfoFocus(int? idPlanCadre, string idRecherche)
        {
            if (idPlanCadre == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (idRecherche != null)
            {
                ViewBag.idRecherche = idRecherche;
            }

            PlanCadre planCadre = db.PlanCadre.Find(idPlanCadre);
            if (planCadre == null)
            {
                return HttpNotFound();
            }


            return View("Info", planCadre);
        }

        [HttpGet]
        public ActionResult Creation(int? idProgramme)
        {
            if (idProgramme == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var typePlanCadres = db.TypePlanCadre.ToList();
            List<SelectListItem> listTypes = new List<SelectListItem>();

            foreach (TypePlanCadre type in typePlanCadres)
            {
                listTypes.Add(new SelectListItem
                {
                    Text = type.Nom,
                    Value = type.IdType.ToString()
                });
            }
            ViewBag.Types = listTypes;

            return View();
        }

        [HttpPost]
        public ActionResult Creation(PlanCadre planCadre)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var planCadreNumero = from DBplanCadre in db.PlanCadre
                where DBplanCadre.NumeroCours.Equals(planCadre.NumeroCours, StringComparison.OrdinalIgnoreCase)
                select DBplanCadre;

            var planCadreTitre = from bdPlanCadre in db.PlanCadre
                where bdPlanCadre.TitreCours.Equals(planCadre.TitreCours, StringComparison.OrdinalIgnoreCase)
                select bdPlanCadre;

            if (planCadreTitre.Any())
            {
                this.AddToastMessage("Titre déjà existant.", planCadreTitre.First().TitreCours + " est déjà entré dans le système.", Toast.ToastType.Error, true);
                ViewBag.Types = new SelectList(db.TypePlanCadre, "idType", "nom", planCadre.IdType);
                return View(planCadre);
            }

            if (planCadreNumero.Any())
            {
                this.AddToastMessage("Numéro déjà utilisée.", "Le numéro de cours « " + planCadreNumero.First().NumeroCours + " » est déjà utilisée pour « " + planCadreNumero.First().TitreCours + " ». Choisissez-en une autre.", Toast.ToastType.Error, true);
                ViewBag.Types = new SelectList(db.TypePlanCadre, "idType", "nom", planCadre.IdType);
                return View(planCadre);
            }

            db.PlanCadre.Add(planCadre);
            db.SaveChanges();
            this.AddToastMessage("Ajout de plan cadre effectué.", "« " + planCadre.TitreCours + " » a été ajouté", Toast.ToastType.Success);

            return RedirectToAction("Choix", new {planCadre.IdPlanCadre});
        }

        [HttpGet]
        public ActionResult Choix(int? idPlanCadre)
        {
            if (idPlanCadre == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            competenceViewModel = new CompetenceViewModel
            {
                EnonceCompetence = db.EnonceCompetence.ToList(), ElementCompetence = db.ElementCompetence.ToList()
            };
            var enonces = db.EnonceCompetence.Select(item => new SelectListItem() {Value = item.IdCompetence.ToString(), Text = (item.CodeCompetence + " : " + item.Description)}).ToList();

            competenceViewModel.PlanCadre = db.PlanCadre.Find(idPlanCadre);
            ViewBag.IdPlanCadre = idPlanCadre;
            competenceViewModel.EnonceCompetences = enonces;
            return View(competenceViewModel);
        }

        public PartialViewResult GetElement(CompetenceViewModel competenceViewModel, int idCompetence)
        {
            competenceViewModel.ElementCompetence = db.ElementCompetence.ToList();
            List<SelectListItem> elements = new List<SelectListItem>();

            var elementEnonce = from element in db.ElementCompetence
                join Enonc in db.EnonceCompetence on element.IdCompetence equals Enonc.IdCompetence
                where Enonc.IdCompetence == idCompetence
                select new
                {
                    ID = element.IdElement,
                    Numero = element.Numero,
                    Desc = element.Description
                };

            foreach (var element in elementEnonce)
            {
                elements.Add(new SelectListItem()
                {
                    Value = element.ID.ToString(),
                    Text = (element.Numero + " : " + element.Desc)
                });
            }

            ViewBag.IdCompetence = idCompetence;
            competenceViewModel.ElementCompetences = elements;
            return PartialView("GetElement", competenceViewModel);
        }


        [HttpPost]
        [WebMethod]
        public ActionResult Choix(string httpBundle, int _idPlanCadre)
        {
            var listPlanCadreEnonceElement = JsonConvert.DeserializeObject<List<PlanCadreCompetenceElement>>(httpBundle);
            foreach (var planCadreEnonceElement in listPlanCadreEnonceElement)
            {
                var planCadreCompetence = new PlanCadreCompetence
                {
                    IdCompetence = planCadreEnonceElement.IdEnonce,
                    IdPlanCadre = _idPlanCadre,
                    //PonderationEnHeure = planCadreEnonceElement.Ponderation
                };
                db.PlanCadreCompetence.Add(planCadreCompetence);
                db.SaveChanges();

                foreach (var element in planCadreEnonceElement.IdElements)
                {
                    var planCadreElement = new PlanCadreElement
                    {
                        IdPlanCadreCompetence = planCadreCompetence.IdPlanCadreCompetence,
                        IdElement = element
                    };
                    db.PlanCadreElement.Add(planCadreElement);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Structure", new { idPlanCadre = _idPlanCadre });
        }

        public ActionResult Structure(int? idPlanCadre)
        {
            if (idPlanCadre == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var structureViewModel = new StructureViewModel();
            var planCadre = db.PlanCadre.Find(idPlanCadre);
            structureViewModel.PlanCadre = planCadre;
            structureViewModel.ElementEnoncePlanCadres = new List<ElementEnoncePlanCadre>();

            var enonces = from enonce in db.EnonceCompetence
                join planCadreCompetence in db.PlanCadreCompetence on enonce.IdCompetence equals planCadreCompetence.IdCompetence
                where planCadreCompetence.IdPlanCadre == planCadre.IdPlanCadre
                select enonce;

            foreach (var enonce in enonces)
            {
                var elements =
                    from element in db.ElementCompetence
                    join planCadreElement in db.PlanCadreElement
                        on element.IdElement equals planCadreElement.IdElement
                    join planCadreCompetence in db.PlanCadreCompetence
                        on planCadreElement.IdPlanCadreCompetence equals planCadreCompetence.IdPlanCadreCompetence
                    where planCadreCompetence.IdCompetence == enonce.IdCompetence && planCadreCompetence.IdPlanCadre == idPlanCadre
                    select element;
                IEnumerable<ElementCompetence> elementCompetences = elements;

                var idPlanCadreCompetences = from planCadreCompetence in db.PlanCadreCompetence
                    where planCadreCompetence.IdPlanCadre == planCadre.IdPlanCadre &&
                          planCadreCompetence.IdCompetence == enonce.IdCompetence
                    select planCadreCompetence.IdPlanCadreCompetence;

                var idPlanCadreCompetence = idPlanCadreCompetences.First();

                structureViewModel.ElementEnoncePlanCadres.Add(new ElementEnoncePlanCadre
                {
                    EnonceCompetence = enonce,
                    ElementCompetences = elementCompetences,
                    IdPlanCadreCompetence = idPlanCadreCompetence
                });
            }

            return View(structureViewModel);
        }
    }
}