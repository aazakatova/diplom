//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Avtoservis.Entities
{
    using System;
    using System.Collections.Generic;
    
    public partial class dm_Detali_v_zakaze
    {
        public int ID_detali_v_zakaze { get; set; }
        public int ID_zakaza { get; set; }
        public int ID_detali { get; set; }
        public int Kolichestvo { get; set; }
        public bool Detal_klienta { get; set; }
        public Nullable<decimal> Zakrep_cena { get; set; }

        public decimal TotalPrice
        {
            get
            {
                if (Detal_klienta)
                    return 0;
                return (Zakrep_cena ?? 0) * Kolichestvo;
            }
        }

        public virtual dm_Detali dm_Detali { get; set; }
        public virtual dm_Zakazi dm_Zakazi { get; set; }
    }
}
