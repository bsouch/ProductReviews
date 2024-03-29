﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProductReviews.CustomExceptionMiddleware;
using ProductReviews.DomainModels;
using ProductReviews.DTOs;
using ProductReviews.Models;
using ProductReviews.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductReviews.Controllers
{
    [Route("api/ProductReviews")]
    [ApiController]
    public class ProductReviewController : ControllerBase
    {
        private readonly IProductReviewsRepository _productReviewsRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheModel _memoryCacheModel;

        public ProductReviewController(IProductReviewsRepository productReviewsRepository, IMapper mapper, IMemoryCache memoryCache,
            IOptions<MemoryCacheModel> memoryCacheModel)
        {
            _productReviewsRepository = productReviewsRepository;
            _mapper = mapper;
            _memoryCache = memoryCache;
            _memoryCacheModel = memoryCacheModel.Value;
        }

        /// <summary>
        /// Get all product reviews.
        /// </summary>
        /// <returns>
        /// An Ok() (Statuscode 200) Object ActionResult alongside a collection of ProductReviewReadDTOs
        /// or an appropriate statuscode based on the exception thrown.
        /// </returns>
        [Authorize("ReadReviews")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductReviewReadDTO>>> GetAllProductReviews()
        {
            if (_memoryCache.TryGetValue(_memoryCacheModel.ProductReviews, out List<ProductReviewModel> productReviewValues))
                return Ok(_mapper.Map<IEnumerable<ProductReviewReadDTO>>(productReviewValues));

            var productReviews = await _productReviewsRepository.GetAllProductReviewsAsync();
            return Ok(_mapper.Map<IEnumerable<ProductReviewReadDTO>>(productReviews));
        }

        /// <summary>
        /// Get all visible product reviews for a particular product.
        /// </summary>
        /// <param name="ID">Represents the product ID to get product reviews for.</param>
        /// <returns>
        /// An Ok() (Statuscode 200) Object ActionResult alongside a collection of ProductReviewReadDTOs
        /// or an appropriate statuscode based on the exception thrown.
        /// </returns>
        [Authorize("ReadVisibleReviews")]
        [Route("Visible/{ID}")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductReviewReadDTO>>> GetAllVisibleProductReviewsForProduct(int ID)
        {
            if(ID < 1)
                throw new ArgumentOutOfRangeException(nameof(ID), "IDs cannot be less than 1.");

            if (_memoryCache.TryGetValue(_memoryCacheModel.ProductReviews, out List<ProductReviewModel> productReviewValues))
                return Ok(_mapper.Map<IEnumerable<ProductReviewReadDTO>>(productReviewValues.Where(pr => !pr.ProductReviewIsHidden && pr.ProductID == ID)));

            var productReviews = await _productReviewsRepository.GetAllVisibleProductReviewsForProductAsync(ID);
            return Ok(_mapper.Map<IEnumerable<ProductReviewReadDTO>>(productReviews));
        }

        /// <summary>
        /// Get a specific product review.
        /// </summary>
        /// <param name="ID">The ID of the required product review.</param>
        /// <returns>
        /// An Ok() (Statuscode 200) Object ActionResult alongside a ProductReviewReadDTO
        /// or an appropriate statuscode based on the exception thrown.
        /// </returns>
        [Authorize("ReadReview")]
        [HttpGet("{ID}")]
        public async Task<ActionResult<ProductReviewReadDTO>> GetProductReview(int ID)
        {
            if (ID < 1)
                throw new ArgumentOutOfRangeException(nameof(ID), "IDs cannot be less than 1.");

            ProductReviewModel productReview;
            //If cache exists and we find the entity.
            if (_memoryCache.TryGetValue(_memoryCacheModel.ProductReviews, out List<ProductReviewModel> productReviewValues))
            {
                //Return the entity if we find it in the cache.
                productReview = productReviewValues.Find(pr => pr.ProductReviewID == ID);
                if (productReview != null)
                    return Ok(_mapper.Map<ProductReviewReadDTO>(productReview));

                //Otherwise, get the entity from the DB, add it to the cache and return it.
                productReview = await _productReviewsRepository.GetProductReviewAsync(ID);
                if (productReview != null)
                {
                    productReviewValues.Add(productReview);
                    return Ok(_mapper.Map<ProductReviewReadDTO>(productReview));
                }

                throw new ResourceNotFoundException("A resource for ID: " + ID + " does not exist.");
            }

            productReview = await _productReviewsRepository.GetProductReviewAsync(ID);

            if (productReview != null)
                return Ok(_mapper.Map<ProductReviewReadDTO>(productReview));

            throw new ResourceNotFoundException("A resource for ID: " + ID + " does not exist.");
        }

        /// <summary>
        /// This function is used to create a product review for a customer for a specific product.
        /// </summary>
        /// <param name="productReviewCreateDTO">The object containing product review properties which will be used to create a review for a product.</param>
        /// <returns>
        /// A CreatedAtAction() (Statuscode 201) ActionResult or an appropriate Statuscode based on the exception thrown.
        /// </returns>
        [Route("Create")]
        [Authorize("CreateReview")]
        [HttpPost]
        public async Task<ActionResult> CreateProductReview([FromBody] ProductReviewCreateDTO productReviewCreateDTO)
        {
            if (productReviewCreateDTO == null)
                throw new ArgumentNullException(nameof(productReviewCreateDTO), "The product review used to update cannot be null.");

            ProductReviewModel productReviewModel = _mapper.Map<ProductReviewModel>(productReviewCreateDTO);
            productReviewModel.ProductReviewDate = System.DateTime.Now;
            productReviewModel.ProductReviewIsHidden = false;

            ProductReviewModel newProductReviewModel = _productReviewsRepository.CreateProductReview(productReviewModel);
            await _productReviewsRepository.SaveChangesAsync();

            if (_memoryCache.TryGetValue(_memoryCacheModel.ProductReviews, out List<ProductReviewModel> productReviewValues))
                productReviewValues.Add(newProductReviewModel);

            ProductReviewReadDTO productReviewReadDTO = _mapper.Map<ProductReviewReadDTO>(newProductReviewModel);

            return CreatedAtAction(nameof(GetProductReview), new { ID = productReviewReadDTO.ProductReviewID }, productReviewReadDTO);
        }

        /// <summary>
        /// This function will update a product review's visibility from visible to hidden and back.
        /// </summary>
        /// <param name="ID">The ID of the review to be hidden.</param>
        /// <param name="productReviewUpdatePatch">The object that contains the new visibilty parameter.</param>
        /// <returns>
        /// A NoContent() (Statuscode 204) ActionResult or an appropriate statuscode based on the exception thrown.
        /// </returns>
        [Route("Visibility/{ID}")]
        [Authorize("UpdateReview")]
        [HttpPatch]
        public async Task<ActionResult> UpdateProductReview(int ID, JsonPatchDocument<ProductReviewUpdateDTO> productReviewUpdatePatch)
        {
            if (ID < 1)
                throw new ArgumentOutOfRangeException(nameof(ID), "IDs cannot be less than 1.");

            if(productReviewUpdatePatch == null)
                throw new ArgumentNullException(nameof(productReviewUpdatePatch), "The product review used to update cannot be null.");

            ProductReviewModel productReviewModel = await _productReviewsRepository.GetProductReviewAsync(ID);
            if (productReviewModel == null)
                throw new ResourceNotFoundException("A resource for ID: " + ID + " does not exist.");

            ProductReviewUpdateDTO newproductReviewRequest = _mapper.Map<ProductReviewUpdateDTO>(productReviewModel);
            productReviewUpdatePatch.ApplyTo(newproductReviewRequest, ModelState);

            if (!TryValidateModel(newproductReviewRequest))
                return ValidationProblem(ModelState);

            _mapper.Map(newproductReviewRequest, productReviewModel);

            _productReviewsRepository.UpdateProductReview(productReviewModel);
            await _productReviewsRepository.SaveChangesAsync();

            if (_memoryCache.TryGetValue(_memoryCacheModel.ProductReviews, out List<ProductReviewModel> productReviewValues))
            {
                productReviewValues.RemoveAll(pr => pr.ProductReviewID == productReviewModel.ProductReviewID);
                productReviewValues.Add(productReviewModel);
            }

            return NoContent();
        }
    }
}
