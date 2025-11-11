using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using PMS.Core.Domain.Entities;
using PMS.Data.DatabaseConfig;
using PMS.Data.Repositories.CustomerDeptRepository;
using PMS.Data.Repositories.CustomerProfile;
using PMS.Data.Repositories.DebtReport;
using PMS.Data.Repositories.GoodReceiptNoteDetailRepository;
using PMS.Data.Repositories.GoodReceiptNoteRepository;
using PMS.Data.Repositories.InventoryHistory;
using PMS.Data.Repositories.InventorySession;
using PMS.Data.Repositories.LotProductRepository;
using PMS.Data.Repositories.Notification;
using PMS.Data.Repositories.PharmacySecretInfor;
using PMS.Data.Repositories.ProductCategoryRepository;
using PMS.Data.Repositories.ProductRepository;
using PMS.Data.Repositories.PurchasingOrderDetailRepository;
using PMS.Data.Repositories.PurchasingOrderRepository;
using PMS.Data.Repositories.PurchasingRequestForQuotationRepository;
using PMS.Data.Repositories.PurchasingRequestProductRepository;
using PMS.Data.Repositories.QuotationDetailRepository;
using PMS.Data.Repositories.QuotationRepository;
using PMS.Data.Repositories.RequestSalesQuotation;
using PMS.Data.Repositories.RequestSalesQuotationDetails;
using PMS.Data.Repositories.SalesOrderDetailsRepository;
using PMS.Data.Repositories.SalesOrderRepository;
using PMS.Data.Repositories.SalesQuotation;
using PMS.Data.Repositories.SalesQuotationComment;
using PMS.Data.Repositories.SalesQuotationDetails;
using PMS.Data.Repositories.SalesQuotationNote;
using PMS.Data.Repositories.StaffProfile;
using PMS.Data.Repositories.StockExportOrder;
using PMS.Data.Repositories.StockExportOrderDetails;
using PMS.Data.Repositories.Supplier;
using PMS.Data.Repositories.TaxPolicy;
using PMS.Data.Repositories.User;
using PMS.Data.Repositories.Warehouse;
using PMS.Data.Repositories.WarehouseLocation;

namespace PMS.Data.UnitOfWork
{
    public class UnitOfWork(PMSContext context,
        IUserRepository users,
        ICustomerProfileRepository customerProfile,
        ISupplierRepository supplier,
        IStaffProfileRepository staffProfile,
        IProductRepository product,
        IProductCategoryRepository category,
        IWarehouseRepository warehouse,
        IWarehouseLocationRepository warehouseLocation,
        INotificationRepository notification,
        IRequestSalesQuotationRepository requestSalesQuotation,
        IRequestSalesQuotationDetailsRepository requestSalesQuotationDetails,
        IPurchasingRequestForQuotationRepository
        purchasingRequestForQuotation,
        IPurchasingRequestProductRepository
        purchasingRequestProduct,
        ILotProductRepository lotProduct, IPurchasingOrderRepository purchasingOrder,
        IPurchasingOrderDetailRepository purchasingOrderDetail,
        IQuotationRepository quotation,
        IQuotationDetailRepository quotationDetail, 
        ISalesQuotationRepository salesQuotation, 
        ISalesQuotationDetailsRepository salesQuotationDetails, 
        ISalesQuotationCommentRepository salesQuotationComment, 
        ITaxPolicyRepository taxPolicy,
        IGoodReceiptNoteDetailRepository goodReceiptNoteDetail,
        IGoodReceiptNoteRepository goodReceiptNote,
        ISalesQuotationNoteRepository salesQuotationNote,
        ISalesOrderRepository salesOrder,
        ISalesOrderDetailsRepository salesOrderDetails,
        ICustomerDeptRepository customerDept, 
        IInventoryHistoryRepository inventoryHistory
        ,IInventorySessionRepository inventorySession, 
        IStockExportOrderRepository stockExportOrder, 
        IStockExportOrderDetailsRepository stockExportOrderDetails,
        IDebtReportRepository debtReport,
        IPharmacySecretInforRepository pharmacySecretInfor) : IUnitOfWork
    {
        private readonly PMSContext _context = context;
        private IDbContextTransaction? _transaction;
        //Users
        public IUserRepository Users { get; private set; } = users;
        public ICustomerProfileRepository CustomerProfile { get; private set; } = customerProfile;
        public IStaffProfileRepository StaffProfile { get; private set; } = staffProfile;
        public ISupplierRepository Supplier { get; private set; } = supplier;
        //Product
        public IProductRepository Product { get; private set; } = product;
        public IProductCategoryRepository Category { get; private set; } = category;
        public ILotProductRepository LotProduct { get; private set; } = lotProduct;
        //Notification
        public INotificationRepository Notification { get; private set; } = notification;
        //Warehouse
        public IWarehouseRepository Warehouse { get; private set; } = warehouse;
        public IWarehouseLocationRepository WarehouseLocation { get; private set; } = warehouseLocation;
        //RequestSalesQuotation
        public IRequestSalesQuotationRepository RequestSalesQuotation { get; private set; } = requestSalesQuotation;
        public IRequestSalesQuotationDetailsRepository RequestSalesQuotationDetails { get; private set; } = requestSalesQuotationDetails;
        //PurchasingRequestForQuotation
        public IPurchasingRequestForQuotationRepository PurchasingRequestForQuotation { get; private set; } = purchasingRequestForQuotation;
        public IPurchasingRequestProductRepository PurchasingRequestProduct { get; private set; } = purchasingRequestProduct;
        //Quotation
        public IQuotationRepository Quotation { get; private set; } = quotation;
        public IQuotationDetailRepository QuotationDetail { get; private set; } = quotationDetail;
        //PurchasingOrder
        public IPurchasingOrderRepository PurchasingOrder { get; private set; } = purchasingOrder;
        public IPurchasingOrderDetailRepository PurchasingOrderDetail { get; private set; } = purchasingOrderDetail;
        //Sales Quotation
        public ISalesQuotationRepository SalesQuotation { get; private set; } = salesQuotation;
        public ISalesQuotationDetailsRepository SalesQuotationDetails { get; private set; } = salesQuotationDetails;
        public ISalesQuotationCommentRepository SalesQuotationComment { get; private set; } = salesQuotationComment;
        public ITaxPolicyRepository TaxPolicy { get; private set; } = taxPolicy;
        //
        public IGoodReceiptNoteDetailRepository GoodReceiptNoteDetail { get; private set; } = goodReceiptNoteDetail;
        public IGoodReceiptNoteRepository GoodReceiptNote { get; private set; } = goodReceiptNote;
        public ISalesQuotationNoteRepository SalesQuotationNote { get; private set; } = salesQuotationNote;
        //SalesOrder
        public ISalesOrderRepository SalesOrder { get; private set; } = salesOrder;
        public ISalesOrderDetailsRepository SalesOrderDetails { get; private set; } = salesOrderDetails;
        //CustomerDept
        public ICustomerDeptRepository CustomerDept { get; private set; } = customerDept;

        //InventoryHistory
        public IInventoryHistoryRepository InventoryHistory { get; private set; } = inventoryHistory;

        //InventorySession
        public IInventorySessionRepository InventorySession { get; private set; } = inventorySession;
        //StockExportOrder
        public IStockExportOrderRepository StockExportOrder { get; private set; } = stockExportOrder;
        public IStockExportOrderDetailsRepository StockExportOrderDetails { get; private set; } = stockExportOrderDetails;
        //DebtReport
        public IDebtReportRepository DebtReport { get; private set; } = debtReport;

        public IPharmacySecretInforRepository PharmacySecretInfor { get; private set; } = pharmacySecretInfor;

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction ??= await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
