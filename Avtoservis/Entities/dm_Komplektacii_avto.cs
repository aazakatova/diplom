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

    public partial class dm_Komplektacii_avto
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public dm_Komplektacii_avto()
        {
            this.dm_Avtomobili = new HashSet<dm_Avtomobili>();
        }
    
        public int ID_komplektacii_avto { get; set; }
        public int Moshnost { get; set; }
        public int Tip_korobki_peredach { get; set; }
        public int Tip_privoda { get; set; }
        public int Tip_dvigatelya { get; set; }
        public int Tip_kuzova { get; set; }
        public int Model_avto { get; set; }
        public string Foto { get; set; }

        public BitmapImage FotoImage { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<dm_Avtomobili> dm_Avtomobili { get; set; }
        public virtual dm_Modeli_avto dm_Modeli_avto { get; set; }
        public virtual dm_Tipi_dvigatelya dm_Tipi_dvigatelya { get; set; }
        public virtual dm_Tipi_korobki_peredach dm_Tipi_korobki_peredach { get; set; }
        public virtual dm_Tipi_kuzova dm_Tipi_kuzova { get; set; }
        public virtual dm_Tipi_privoda dm_Tipi_privoda { get; set; }
    }
}
