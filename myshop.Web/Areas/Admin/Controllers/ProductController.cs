using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using myshop.DataAccess;
using myshop.Entities.Models;
using myshop.Entities.Repositories;
using myshop.Entities.ViewModels;

namespace myshop.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var products = _unitOfWork.Product.GetAll(Includeword: "Category");
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ProductVM productVM = new()
            {
                Product = new(),
                CategoryList = _unitOfWork.Category.GetAll()
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString()
                    })
            };
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVM productVM, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null && file.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string uploads = Path.Combine(wwwRootPath, "images", "products");

                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    string filePath = Path.Combine(uploads, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // استخدم مسار نسبي بدون ~ أو / في البداية
                    productVM.Product.Img = "images/products/" + fileName;
                }
                else
                {
                    productVM.Product.Img = "images/default-product.png";
                }

                _unitOfWork.Product.Add(productVM.Product);
                _unitOfWork.Complete();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index");
            }

            productVM.CategoryList = _unitOfWork.Category.GetAll()
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                });

            return View(productVM);
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var product = _unitOfWork.Product.GetFirstorDefault(x => x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ProductVM productVM = new()
            {
                Product = product,
                CategoryList = _unitOfWork.Category.GetAll()
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString()
                    })
            };

            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    string uploads = Path.Combine(wwwRootPath, "images", "products");
                    string extension = Path.GetExtension(file.FileName);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(productVM.Product.Img))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.Img.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Upload new image
                    using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productVM.Product.Img = "/images/products/" + fileName + extension;
                }

                _unitOfWork.Product.Update(productVM.Product);
                _unitOfWork.Complete();
                TempData["success"] = "Product updated successfully";
                return RedirectToAction("Index");
            }

            return View(productVM);
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var product = _unitOfWork.Product.GetFirstorDefault(x => x.Id == id, Includeword: "Category");
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var product = _unitOfWork.Product.GetFirstorDefault(x => x.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Delete image if exists
            if (!string.IsNullOrEmpty(product.Img))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                var imagePath = Path.Combine(wwwRootPath, product.Img.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _unitOfWork.Product.Remove(product);
            _unitOfWork.Complete();
            TempData["success"] = "Product deleted successfully";
            return RedirectToAction("Index");
        }
    }
}