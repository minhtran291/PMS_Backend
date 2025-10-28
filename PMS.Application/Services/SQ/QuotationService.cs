﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.SQ;
using PMS.Application.Services.Base;
using PMS.Core.Domain.Constant;
using PMS.Core.Domain.Enums;
using PMS.Data.UnitOfWork;

namespace PMS.API.Services.QuotationService
{
    public class QuotationService(IUnitOfWork unitOfWork, IMapper mapper) : Service(unitOfWork, mapper), IQuotationService
    {
        public async Task<ServiceResult<IEnumerable<QuotationDTO>>> GetAllQuotationAsync()
        {
            try
            {
                var quotations = await _unitOfWork.Quotation.Query().ToListAsync();

                if (quotations == null || !quotations.Any())
                {
                    return ServiceResult<IEnumerable<QuotationDTO>>.Fail("Không có báo giá nào trong hệ thống", 404);
                }

                var now = DateTime.Now;

                var result = quotations.Select(q => new QuotationDTO
                {
                    QID = q.QID,
                    SendDate = q.SendDate,
                    SupplierID = q.SupplierID,
                    QuotationExpiredDate = q.QuotationExpiredDate,

                   
                    Status = q.QuotationExpiredDate >= now
                        ? SupplierQuotationStatus.InDate
                        : SupplierQuotationStatus.OutOfDate
                });

                return ServiceResult<IEnumerable<QuotationDTO>>.SuccessResult(result, "Lấy danh sách báo giá thành công", 200);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<QuotationDTO>>.Fail($"Lỗi khi lấy danh sách báo giá: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResult<List<QuotationDTO>>> GetAllQuotationsWithActiveDateAsync()
        {
            try
            {
                var quotations = await _unitOfWork.Quotation.Query().ToListAsync();

                if (quotations == null || !quotations.Any())
                {
                    return ServiceResult<List<QuotationDTO>>.Fail("Không có báo giá nào trong hệ thống", 404);
                }

                var now = DateTime.Now;

                var result = quotations.Select(q => new QuotationDTO
                {
                    QID = q.QID,
                    SendDate = q.SendDate,
                    SupplierID = q.SupplierID,
                    QuotationExpiredDate = q.QuotationExpiredDate,
                    Status = q.QuotationExpiredDate >= now
                        ? SupplierQuotationStatus.InDate
                        : SupplierQuotationStatus.OutOfDate
                }).ToList();

                return ServiceResult<List<QuotationDTO>>.SuccessResult(result, "Lấy danh sách báo giá kèm trạng thái hạn thành công", 200);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<QuotationDTO>>.Fail($"Lỗi khi lấy danh sách báo giá: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResult<QuotationDTO?>> GetQuotationByIdAsync(int id)
        {
            try
            {
                
                var quotation = await _unitOfWork.Quotation.Query()
                    .Include(q => q.QuotationDetails)                      
                    .FirstOrDefaultAsync(q => q.QID == id);

                if (quotation == null)
                {
                    return ServiceResult<QuotationDTO?>.Fail($"Không tìm thấy báo giá với ID: {id}", 404);
                }

                var now = DateTime.Now;


                var result = new QuotationDTO
                {
                    QID = quotation.QID,
                    SendDate = quotation.SendDate,
                    SupplierID = quotation.SupplierID,
                    QuotationExpiredDate = quotation.QuotationExpiredDate,
                    Status = quotation.QuotationExpiredDate >= now
                        ? SupplierQuotationStatus.InDate
                        : SupplierQuotationStatus.OutOfDate,


                    QuotationDetailDTOs = quotation.QuotationDetails.Select(d => new QuotationDetailDTO
                    {
                        ProductID = d.ProductID,
                        ProductName = d.ProductName,
                        ProductDescription = d.ProductDescription,
                        ProductUnit = d.ProductUnit,
                        UnitPrice = d.UnitPrice,
                        ProductDate = d.ProductDate
                    }).ToList()
                };

                return ServiceResult<QuotationDTO?>.SuccessResult(result, "Lấy thông tin báo giá thành công", 200);
            }
            catch (Exception ex)
            {
                return ServiceResult<QuotationDTO?>.Fail($"Lỗi khi lấy thông tin báo giá: {ex.Message}", 500);
            }
        }

    }
}
