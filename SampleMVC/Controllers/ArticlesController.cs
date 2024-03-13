using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyWebFormApp.BLL;
using MyWebFormApp.BLL.DTOs;
using MyWebFormApp.BLL.Interfaces;
using SampleMVC.Helpers;
using SampleMVC.ViewModels;
using System.Text.Json;

namespace SampleMVC.Controllers
{
    public class ArticlesController : Controller
    {
        private UserDTO user = null;

        private readonly ICategoryBLL _categoryBLL;
        private readonly IArticleBLL _articleBLL;

        public ArticlesController(ICategoryBLL categoryBLL, IArticleBLL articleBLL)
        {
            _categoryBLL = categoryBLL;
            _articleBLL = articleBLL;
        }

        public IActionResult Index(int CategoryID)
        {
            if (HttpContext.Session.GetString("user") == null)
            {
                TempData["message"] = @"<div class='alert alert-danger'><strong>Error!</strong>Anda harus login terlebih dahulu !</div>";
                return RedirectToAction("Login", "Users");
            }
            user = JsonSerializer.Deserialize<UserDTO>(HttpContext.Session.GetString("user"));
            //pengecekan session username
            if (Auth.CheckRole("reader", user.Roles.ToList()) == false)
            {
                TempData["message"] = @"<div class='alert alert-danger'><strong>Error!</strong>Anda tidak memiliki hak akses !</div>";
                return RedirectToAction("Index", "Home");
            }

            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"]?.ToString();
            }

            ArticlesByCategoryViewModel articlesByCategoryViewModel = new ArticlesByCategoryViewModel();
            articlesByCategoryViewModel.Categories = new SelectList(_categoryBLL.GetAll(), "CategoryID", "CategoryName");
            articlesByCategoryViewModel.Articles = _articleBLL.GetWithPaging(CategoryID, 1, 3).ToList();

            return View(articlesByCategoryViewModel);
        }

        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("user") == null)
            {
                TempData["message"] = @"<div class='alert alert-danger'><strong>Error!</strong>Anda harus login terlebih dahulu !</div>";
                return RedirectToAction("Login", "Users");
            }
            user = JsonSerializer.Deserialize<UserDTO>(HttpContext.Session.GetString("user"));
            //pengecekan session username
            if (Auth.CheckRole("contributor", user.Roles.ToList()) == false)
            {
                TempData["message"] = @"<div class='alert alert-danger'><strong>Error!</strong>Anda tidak memiliki hak akses !</div>";
                return RedirectToAction("Index", "Home");
            }

            var categories = _categoryBLL.GetAll();

            var listCategories = new SelectList(categories, "CategoryID", "CategoryName");
            ViewBag.Categories = listCategories;
            return View();
        }

        [HttpPost]
        public IActionResult Create(ArticleCreateDTO articleCreateDTO, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                if (file != null)
                {
                    if (Helper.IsImageFile(file.FileName))
                    {
                        //random file name based on GUID
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pics", fileName);
                        articleCreateDTO.Pic = fileName;
                        _articleBLL.Insert(articleCreateDTO);

                        //copy file ke folder
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }
                        TempData["Message"] = @"<div class='alert alert-success'><strong>Success!&nbsp;</strong>Article has been added successfully !</div>";
                    }
                    else
                        TempData["Message"] = @"<div class='alert alert-danger'><strong>Error!&nbsp;</strong>File is not an image file !</div>";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = @"<div class='alert alert-danger'><strong>Error!&nbsp;</strong>" + ex.Message + "</div>";
            }

            return RedirectToAction("Index", new { CategoryID = articleCreateDTO.CategoryID });

            //return Content($"{articleCreateDTO.CategoryID} - {articleCreateDTO.Title} - {articleCreateDTO.Details} - {articleCreateDTO.IsApproved}");
            //return View(articleCreateDTO);
        }
    }
}
