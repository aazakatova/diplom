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
    
    public partial class dm_Raboti
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public dm_Raboti()
        {
            this.dm_Raboti_v_zakaze = new HashSet<dm_Raboti_v_zakaze>();
        }
    
        public int ID_raboti { get; set; }
        public string Naimenovanie { get; set; }
        public decimal Stoimost { get; set; }
        public Nullable<int> Dlitelnost { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<dm_Raboti_v_zakaze> dm_Raboti_v_zakaze { get; set; }
    }
}
