   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5   	       Avtoservis.AddEdit_Raboti   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:Avtoservis     5   	     � Title$D  >Добавление / Редактирование работ��5   	   $	��450��63   $	��700��6@   $	��450��6L   $	��700��6\    � WindowStartupLocation$ CenterScreen=�6k   $��16(�6�   $��Century Gothic)�6�   .��5	      �  � RowDefinitions 5
   
   �� 5      $��70 �6   6*   �� 5      $��* �6   6)   .+�5   
   � $��0��6   ���#282B3863   �� 5   
   -   5         ��Name$ TextBlockZagolovok��5      $9��3Добавление / редактирование��60   $��20(�6S   $��Center=�6a   $	��Top=�6~   $
��Bold"�6�   $
��auto��6�   $��28��6�    �� TextAlignment$ Center=�6�   $��0,25,0,0q�6�   
�������6�   �� 5   
   $��Center=�6   $��Center=�62   $��	0,-20,0,0q�6M   $��1��6`   $
��auto��6m   $
��auto��6z    � Cursor$  ��6�   .+�5      ��  �� Orientation$ 
Horizontal=�6   $
��0,10q�62   .+�5      �� $	��170��6   $��Center=�6(   $��Center=�6E   �� 5      $�Наименование��6   
���   �62    �� 5      $�*��6   ���	#FF567BFF6'   5      �� 5      -   5         ��Name$ TextBoxNazvanie��5      $	��300��63   $��35��6?   $��10,0,0,0q�6K   $��8,4,0,0q�6]   $��16(�6o   6   5      �� 5      $ 
Horizontal=�6   $
��0,10q�62   .+�5      �� $	��170��6   $��Center=�6(   $��Center=�6E   �� 5      $�Цена��6   
���   �6*    �� 5      $�*��6   ���	#FF567BFF6'   5      �� 5       -   5        $ TextBoxCena��5       $	��300��6/   $��10,0,0,0q�6;   $��35��6M   $��8,4,0,0q�6Y   $��16(�6k   6{   5!      �� 5"      $ 
Horizontal=�6   $
��0,10q�62   .+�5#      �� $	��170��6   $��Center=�6(   $��Center=�6E   �� 5$      $'�!Длительность, мин.��6   
���   �68    �� 5%      $�*��6   ���	#FF567BFF6'   5&      �� 5'      -   5        $ TextBoxDlitelnost��5'      $	��300��65   $��35��6A   $��10,0,0,0q�6M   $��8,4,0,0q�6_   $��16(�6q   6�   5(      �� 5)      -   5        $��BtnSave��5)   ?   $��40��6   $	��250��6!   $��0,20,0,0q�6-   $��Center=�6N   $��Bottom=�6k   $��Сохранить��5*      $��1��6)      LoginButtonStyle#��[  66   6b   5+      5-      5.      