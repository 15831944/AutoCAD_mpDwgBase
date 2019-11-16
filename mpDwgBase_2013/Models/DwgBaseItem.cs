namespace mpDwgBase.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Элемент базы
    /// </summary>
    [Serializable]
    public class DwgBaseItem
    {
        /// <summary>
        /// Кто добавил блок в базу
        /// </summary>
        [XmlAttribute]
        public string Author { get; set; }

        /// <summary>
        /// Источник
        /// </summary>
        [XmlAttribute]
        public string Source { get; set; }

        /// <summary>
        /// Название блока (узла)
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Путь
        /// </summary>
        [XmlAttribute]
        public string Path { get; set; }

        /// <summary>
        /// Файл-источник
        /// </summary>
        [XmlAttribute]
        public string SourceFile { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        [XmlAttribute]
        public string Description { get; set; }

        /// <summary>
        /// Нормативный документ (если есть)
        /// </summary>
        [XmlAttribute]
        public string Document { get; set; }

        /// <summary>
        /// Аннотативный?
        /// </summary>
        [XmlAttribute]
        public bool IsAnnotative { get; set; }

        /// <summary>
        /// Динамический?
        /// </summary>
        [XmlAttribute]
        public bool IsDynamicBlock { get; set; }

        /// <summary>
        /// Есть ли атрибуты для спецификации
        /// </summary>
        [XmlAttribute]
        public bool HasAttributesForSpecification { get; set; }

        /// <summary>
        /// Является ли блоком
        /// </summary>
        [XmlAttribute]
        public bool IsBlock { get; set; }

        /// <summary>
        /// Имя блока в файле-исходнике
        /// </summary>
        [XmlAttribute]
        public string BlockName { get; set; }

        /// <summary>
        /// Является ли блок трехмерным
        /// </summary>
        [XmlAttribute]
        public bool Is3Dblock { get; set; }

        /// <summary>
        /// Атрибут - Обозначение
        /// </summary>
        [XmlAttribute]
        public string PositionValue { get; set; }

        /// <summary>
        /// Атрибут - Обозначение
        /// </summary>
        [XmlAttribute]
        public string DesignationValue { get; set; }

        /// <summary>
        /// Атрибут - Наименование
        /// </summary>
        [XmlAttribute]
        public string NominationValue { get; set; }

        /// <summary>
        /// Атрибут - масса
        /// </summary>
        [XmlAttribute]
        public string MassValue { get; set; }

        /// <summary>
        /// Атрибут - примечание
        /// </summary>
        [XmlAttribute]
        public string NoteValue { get; set; }

        /// <summary>
        /// Значение атрибутов, входящих в сам блок
        /// </summary>
        [XmlElement(IsNullable = true)]
        public List<AttributeValue> AttributesValues { get; set; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var dwgBaseItem = (DwgBaseItem)obj;

            return
                BlockName == dwgBaseItem.BlockName &&
                Source == dwgBaseItem.Source &&
                SourceFile == dwgBaseItem.SourceFile &&
                Name == dwgBaseItem.Name &&
                Description == dwgBaseItem.Description &&
                Document == dwgBaseItem.Document &&
                Path == dwgBaseItem.Path;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }
    }
}