﻿using Agri.Data;
using Agri.Models.Configuration;
using Agri.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SERVERAPI.Filters;
using SERVERAPI.Models.Impl;
using SERVERAPI.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace SERVERAPI.Controllers
{
    [SessionTimeout]
    public class FarmController : BaseController
    {
        private readonly UserData _ud;
        private readonly IAgriConfigurationRepository _sd;
        private readonly IOptions<AppSettings> _appSettings;

        public FarmController(UserData ud,
            IAgriConfigurationRepository sd,
            IOptions<AppSettings> appSettings)
        {
            _ud = ud;
            _sd = sd;
            _appSettings = appSettings;
        }

        [HttpGet]
        public IActionResult Farm()
        {
            var farmData = _ud.FarmDetails();
            FarmViewModel fvm = new FarmViewModel();
            fvm.IsLegacyNMPReleaseVersion = !_ud.FarmData().NMPReleaseVersion.HasValue || _ud.FarmData().NMPReleaseVersion.Value < 2;

            if (fvm.IsLegacyNMPReleaseVersion)
            {
                fvm.LegacyNMPMessage = _sd.GetUserPrompt("FarmDataBackwardsCompatibility");
            }
            fvm.ShowSubRegion = false;

            fvm.RegOptions = _sd.GetRegionsDll().ToList();
            fvm.SelRegOption = null;

            fvm.Year = farmData.Year;
            fvm.CurrentYear = farmData.Year;
            fvm.FarmName = farmData.FarmName;

            fvm.SelRegOption = farmData.FarmRegion;
            fvm.SelSubRegOption = farmData.FarmSubRegion;

            if (farmData.HasAnimals &&
                (farmData.HasBeefCows || farmData.HasDairyCows || farmData.HasMixedLiveStock))
            {
                fvm = SetSubRegions(fvm);
            }

            if (fvm.IsLegacyNMPReleaseVersion && fvm.SelSubRegOption.HasValue && farmData.HasAnimals)
            {
                _ud.UpdateFarmDetailsSubRegion(fvm.SelSubRegOption.Value);
            }
            else
            {
                _ud.SetLegacyDataToUnsaved();
            }

            fvm.HasAnimals = farmData.HasAnimals;
            fvm.HasDairyCows = farmData.HasDairyCows;
            fvm.HasBeefCows = farmData.HasBeefCows;
            fvm.HasPoultry = farmData.HasPoultry;
            fvm.HasMixedLiveStock = farmData.HasMixedLiveStock;

            if (fvm.HasAnimals)
            {
                fvm.ShowAnimals = true;
            }
            else
            {
                fvm.ShowAnimals = false;
            }

            return View(fvm);
        }

        private FarmViewModel SetSubRegions(FarmViewModel fvm)
        {
            return SetSubRegionsForRegion(fvm) as FarmViewModel;
        }

        private FarmIncompleteViewModel SetSubRegions(FarmIncompleteViewModel fvm)
        {
            return SetSubRegionsForRegion(fvm) as FarmIncompleteViewModel;
        }

        private FarmViewModelBase SetSubRegionsForRegion(FarmViewModelBase fvm)
        {
            if (fvm.SelRegOption.HasValue)
            {
                fvm.SubRegionOptions = _sd.GetSubRegionsDll(fvm.SelRegOption);
            }
            fvm.ShowSubRegion = true;
            if (fvm.SelSubRegOption == null)
            {
                ModelState.AddModelError("", "Select a sub region");
            }

            return fvm;
        }

        private void UpdateStorageSystemsForSubRegion(int subRegionId)
        {
            var storageSystems = _ud.GetStorageSystems();

            if (storageSystems.Count() > 0)
            {
                foreach (var s in storageSystems)
                {
                    if (s.AnnualPrecipitation != null)
                    {
                        SubRegion subregion = _sd.GetSubRegion(subRegionId);
                        s.AnnualPrecipitation = subregion.AnnualPrecipitation;
                    }

                    _ud.UpdateManureStorageSystem(s);
                }
            }
        }

        [HttpPost]
        public IActionResult Farm(FarmViewModel fvm)
        {
            fvm.RegOptions = _sd.GetRegionsDll().ToList();

            if (fvm.ButtonPressed == "GetsAnimalsChange")
            {
                ModelState.Clear();
                fvm.ButtonPressed = "";

                if (fvm.HasAnimals)
                {
                    fvm.ShowAnimals = true;
                    fvm = SetSubRegions(fvm);
                }
                else
                {
                    fvm.ShowAnimals = false;
                }

                var farmData = _ud.FarmDetails();
                farmData.HasSelectedFarmType = fvm.HasSelectedFarmType;
                farmData.HasAnimals = fvm.HasAnimals;
                farmData.HasDairyCows = fvm.HasDairyCows;
                farmData.HasBeefCows = fvm.HasBeefCows;
                farmData.HasPoultry = fvm.HasPoultry;
                farmData.HasMixedLiveStock = fvm.HasMixedLiveStock;
                _ud.UpdateFarmDetails(farmData);

                return View(fvm);
            }

            if (fvm.ButtonPressed == "RegionChange")
            {
                ModelState.Clear();
                fvm.ButtonPressed = "";
                if (fvm.ShowAnimals)
                {
                    fvm = SetSubRegions(fvm);
                }

                var farmData = _ud.FarmDetails();
                farmData.FarmRegion = fvm.SelRegOption;
                _ud.UpdateFarmDetails(farmData);

                if (fvm.ShowAnimals)
                {
                    return View(fvm);
                }

                return View(fvm);
            }

            if (fvm.ButtonPressed == "SubRegionChange")
            {
                ModelState.Clear();
                fvm.ButtonPressed = "";
                fvm = SetSubRegions(fvm);

                UpdateStorageSystemsForSubRegion(fvm.SelSubRegOption.Value);

                var farmData = _ud.FarmDetails();
                farmData.FarmSubRegion = fvm.SelSubRegOption;
                _ud.UpdateFarmDetails(farmData);

                return View(fvm);
            }

            if (fvm.SelRegOption == 0)
            {
                ModelState.AddModelError("SelRegOption", "Select a region");
                return View(fvm);
            }

            if (fvm.ShowAnimals &&
                (fvm.HasBeefCows || fvm.HasDairyCows || fvm.HasMixedLiveStock) &&
                (fvm.SelSubRegOption == null || fvm.SelSubRegOption == 0))
            {
                ModelState.AddModelError("SelSubRegOption", "Select a sub region");
                fvm = SetSubRegions(fvm);
                return View(fvm);
            }

            if (ModelState.IsValid)
            {
                fvm.HasSelectedFarmType = true;
                var farmData = _ud.FarmDetails();

                fvm.RegOptions = _sd.GetRegionsDll().ToList();

                farmData.Year = fvm.Year;
                farmData.FarmName = fvm.FarmName;
                farmData.FarmRegion = fvm.SelRegOption;
                if (fvm.ShowAnimals)
                {
                    fvm.SubRegionOptions = _sd.GetSubRegionsDll(fvm.SelRegOption);
                    if (fvm.SubRegionOptions.Count == 1)
                    {
                        fvm.SelSubRegOption = fvm.SubRegionOptions[0].Id;
                    }
                    farmData.FarmSubRegion = fvm.SelSubRegOption;
                }

                farmData.HasSelectedFarmType = fvm.HasSelectedFarmType;
                farmData.HasAnimals = fvm.HasAnimals;
                farmData.HasDairyCows = fvm.HasDairyCows;
                farmData.HasBeefCows = fvm.HasBeefCows;
                farmData.HasPoultry = fvm.HasPoultry;
                farmData.HasMixedLiveStock = fvm.HasMixedLiveStock;

                _ud.UpdateFarmDetails(farmData);
                HttpContext.Session.SetObject("Farm", _ud.FarmDetails().FarmName + " " + _ud.FarmDetails().Year);

                fvm.CurrentYear = fvm.Year;
                ModelState.Remove("userData");

                //Navigate to next item past Farm
                var journey = _ud.FarmDetails().UserJourney;
                var initialNavigation = _sd.GetJourney((int)journey)
                    .MainMenus
                    .Single(m => m.SortNumber == 2);

                if (initialNavigation.UsesFeaturePages)
                {
                    return RedirectToPage(initialNavigation.Page);
                }
                else
                {
                    return RedirectToAction(initialNavigation.Action, initialNavigation.Controller);
                }
            }
            else
            {
                if (fvm.ShowAnimals)
                {
                    fvm.SubRegionOptions = _sd.GetSubRegionsDll(fvm.SelRegOption);
                }
                return View(fvm);
            }
        }

        [HttpGet]
        public JsonResult SubRegions(int id)
        {
            var result = new List<SelectListItem>();

            var farmData = _ud.FarmDetails();
            farmData.FarmRegion = id;
            _ud.UpdateFarmDetails(farmData);

            if (id > 0)
            {
                result = _sd.GetSubRegionsDll(id).OrderBy(sr => sr.Value).ToList();
            }

            result.Insert(0, new SelectListItem
            {
                Id = 0,
                Value = "select subregion"
            });
            return new JsonResult(result);
        }

        [HttpPost]
        public JsonResult FarmSubRegion(int id)
        {
            UpdateStorageSystemsForSubRegion(id);

            var farmData = _ud.FarmDetails();
            farmData.FarmSubRegion = id;
            _ud.UpdateFarmDetails(farmData);

            return new JsonResult("");
        }

        [HttpGet]
        public object CheckCompleted()
        {
            var journey = _ud.FarmDetails().UserJourney;
            //fvm.HasBeefCows || fvm.HasDairyCows || fvm.HasMixedLiveStock
            var regionsIncomplete = false;

            if (journey != Agri.Models.UserJourney.Crops)
            {
                var regionId = _ud.FarmDetails().FarmRegion;
                regionsIncomplete = !regionId.HasValue;

                if (regionId.HasValue)
                {
                    regionsIncomplete = _sd.GetRegion(regionId.Value) == null;
                }

                if (!regionsIncomplete &&
                    (journey == Agri.Models.UserJourney.Dairy ||
                        journey == Agri.Models.UserJourney.Mixed ||
                        journey == Agri.Models.UserJourney.Ranch)
                    )
                {
                    var subRegionId = _ud.FarmDetails().FarmSubRegion;
                    if (subRegionId.HasValue)
                    {
                        regionsIncomplete = _sd.GetSubRegion(subRegionId.Value) == null;
                    }
                    else
                    {
                        regionsIncomplete = true;
                    }
                }
            }

            var result = new { incomplete = regionsIncomplete.ToString() };
            return result;
        }

        [HttpGet]
        public IActionResult FarmIncomplete(string target)
        {
            var farmData = _ud.FarmDetails();

            var fvm = new FarmIncompleteViewModel
            {
                Message = _sd.GetUserPrompt("RegionRequiredWarning"),
                Target = target
            };

            fvm.SelRegOption = farmData.FarmRegion;
            if (farmData.HasAnimals)
            {
                if (!farmData.HasPoultry)

                {
                    fvm.SelSubRegOption = farmData.FarmSubRegion;
                    fvm = SetSubRegions(fvm);
                }
                else
                {
                    fvm.ShowSubRegion = false;
                }
                fvm.ShowAnimals = true;
            }
            else
            {
                fvm.ShowAnimals = false;
            }
            fvm.RegOptions = _sd.GetRegionsDll().ToList();

            return View(fvm);
        }

        [HttpPost]
        public IActionResult FarmIncomplete(FarmIncompleteViewModel fvm)
        {
            fvm.RegOptions = _sd.GetRegionsDll().ToList();

            if (fvm.ButtonPressed == "RegionChange")
            {
                ModelState.Clear();
                fvm.ButtonPressed = "";
                if (fvm.ShowAnimals && fvm.ShowSubRegion)
                {
                    fvm = SetSubRegions(fvm);
                }

                var farmData = _ud.FarmDetails();
                farmData.FarmRegion = fvm.SelRegOption;
                _ud.UpdateFarmDetails(farmData);

                if (fvm.ShowAnimals)
                {
                    return View(fvm);
                }

                if (fvm.ShowAnimals && fvm.ShowSubRegion)
                {
                    fvm.ButtonPressed = "SubRegionChange";
                }
            }

            if (fvm.ButtonPressed == "SubRegionChange")
            {
                ModelState.Clear();
                fvm.ButtonPressed = "";

                fvm = SetSubRegions(fvm);

                UpdateStorageSystemsForSubRegion(fvm.SelSubRegOption.Value);

                var farmData = _ud.FarmDetails();
                farmData.FarmSubRegion = fvm.SelSubRegOption;
                _ud.UpdateFarmDetails(farmData);
                return View(fvm);
            }

            if (ModelState.IsValid)
            {
                return Json(new { success = true, url = fvm.Target });
            }

            if (fvm.ShowAnimals)
            {
                fvm.SubRegionOptions = _sd.GetSubRegionsDll(fvm.SelRegOption);
            }

            return View(fvm);
        }
    }
}