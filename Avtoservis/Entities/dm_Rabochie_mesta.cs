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
    using System.Windows.Media.Imaging;

    public partial class dm_Rabochie_mesta
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public dm_Rabochie_mesta()
        {
            this.dm_Zakazi = new HashSet<dm_Zakazi>();
        }
    
        public int ID_rabochego_mesta { get; set; }
        public string Rabochee_mesto { get; set; }
        public string Icon { get; set; }

        public BitmapImage IconImage { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<dm_Zakazi> dm_Zakazi { get; set; }
    }
}
