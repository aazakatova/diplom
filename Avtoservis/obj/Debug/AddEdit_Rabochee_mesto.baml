   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5   	   '    !Avtoservis.AddEdit_Rabochee_mesto   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:Avtoservis     5   	     � Title$Q  KДобавление / Редактирование рабочих мест��5   	   $	��550��6:   $	��600��6G   $	��550��6S   $	��600��6c    � WindowStartupLocation$ CenterScreen=�6r   $��16(�6�   $��Century Gothic)�6�   .��5	      �  � RowDefinitions 5
   
   �� 5      $��70 �6   6*   �� 5      $��* �6   6)   .+�5   
   � $��0��6   ���#282B3863   �� 5   
   -   5         ��Name$ PopupCanvas��5       K� ZIndex$
 1000��6&   �� 5   
   -   5         ��Name$ TextBlockZagolovok��5      $9��3Добавление / редактирование��60   $��20(�6S   $��Center=�6a   $	��Top=�6~   $
��Bold"�6�   $
��auto��6�   $��28��6�    �� TextAlignment$ Center=�6�   $��0,25,0,0q�6�   
�������6�   � 5   
   $��1��6   $��	0,-20,0,0q�6   $��Center=�6/   $��Center=�6L   $
��auto��6g   $
��auto��6t   .+�5      �� $��Center=�6   $	��Top=�66   $��0,10,0,0q�6N   .+�5      �� $��Center=�6   $	��Top=�6:   $��0,0,0,20q�6R   .+�5      ��  �� CornerRadius$ 10��6   ���	#FF567BFF$��1q�5      $	��182��5      $	��182��5      $	��Top=�5      
�������._�5"      �  J� Clip 5#      �� 5$   "   	 �� Rect$	 0,0,180,180�64   
 �� RadiusX$
 10Q�6G    �� RadiusY$ 10Q�6T   .+�5(      �� -   5         ��Name$ ImageService��5(   $   $��UniformToFill=�6:   $	��150��6R   $	��150��6^   �� 5*      -   5         ��Name$ ImagePlaceholderBorder��5*   %   
�������y�6X   ��  �� ElementName$ ImagePlaceholder�� �� Path$ 
Visibility�._�5+   "   �� -   5        $ ImagePlaceholder��5+   ,   $��Иконка��6F   
���   �$��Center=�6g   $��Center=�6�   $��16(�6�   $
��Bold"�6�   5,      5-      5.      �� 5/      -   5        $��BtnSelectImage��5/   l      SelectImageButtonStyle#��[  6   $	��180��6M   $��0,10,0,0q�6Z   $��Center=�6�   $	��Top=�6�   $!��Выбрать иконку��50   ;   $��15(�6T   51      �� 54       �� Orientation$ 
Horizontal=�6   $	��0,5q�66   .+�55      �� $	��110��6    $��Center=�6,   $��Center=�6I   �� 56      $�Название��6   
���   �6.    �� 57      $�*��6   ���	#FF567BFF6'   58      �� 59      -   5         ��Name$ TextBoxNazvanie��59      $	��300��67   $��35��6C   $��10,0,0,0q�6O   $��8,4,0,0q�6a   $��16(�6s   5:      �� 5<      -   5        $��BtnSave��5<   C   $��40��6   $	��250��6%   $��0,20,0,0q�61   $��Center=�6R   $��Bottom=�6o   $��Сохранить��5=      $��1��6)     LoginButtonStyle#��[ 66   6b   5>      5?      5A      5B      