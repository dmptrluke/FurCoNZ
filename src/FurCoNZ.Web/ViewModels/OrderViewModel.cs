﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FurCoNZ.Web.Models;

namespace FurCoNZ.Web.ViewModels
{
    public class OrderViewModel
    {
        public OrderViewModel() { }

        public OrderViewModel(Order order)
        {
            Id = order.Id;
            OrderedBy = order.OrderedBy;
            Tickets = order.TicketsPurchased.Select(t => new TicketDetailViewModel(t)).ToList();
            if (order.Audits != null)
            {
                Audits = order.Audits.Select(a => new OrderAuditViewModel(a)).ToList();
            }
            AmountTotalCents = order.TotalAmountCents;
            AmountOwingCents = order.AmountOwingCents;
            AmountPaidCents = order.AmountPaidCents;
        }

        public int Id { get; set; }

        public User OrderedBy { get; set; }

        public decimal AmountTotal => (decimal)AmountTotalCents / 100;
        public decimal AmountPaid => (decimal)AmountPaidCents / 100;
        public decimal AmountOwing => (decimal)AmountOwingCents / 100;

        public ICollection<TicketDetailViewModel> Tickets { get; set; } = new List<TicketDetailViewModel>();
        public ICollection<OrderAuditViewModel> Audits { get; set; } = new List<OrderAuditViewModel>();
        public int AmountTotalCents { get; set; }
        public int AmountOwingCents { get; set; }
        public int AmountPaidCents { get; set; }

        public string Status
        {
            get
            {
                if (Audits.Any(a => a.Type == nameof(AuditType.Refunded)))
                    return "Refunded";

                if (AmountOwing <= 0)
                    return "Paid";

                return "Owing";
            }
        }
    }
}
