   M S B A M L   `   `   `  ����E  AAvtoservis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:Avtoservis
Avtoservis  5   	       Avtoservis.AddEdit_Klient   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:Avtoservis     5   	     � Title$J  DДобавление / Редактирование клиентов��5   	   $	��550��66   $	��750��6C   $	��550��6O   $	��750��6_    � WindowStartupLocation$ CenterScreen=�6n   $��16(�6�   $��Century Gothic)�6�   .��5	      �  � RowDefinitions 5
   
   �� 5      $��70 �6   6*   �� 5      $��* �6   6)   .+�5   
   � $��0��6   ���#282B3863   �� 5   
   -   5         ��Name$ PopupCanvas��5       K� ZIndex$
 1000��6&   �� 5   
   -   5         ��Name$ TextBlockZagolovok��5      $9��3Добавление / редактирование��60   $��20(�6S   $��Center=�6a   $	��Top=�6~   $
��Bold"�6�   $
��auto��6�   $��28��6�    �� TextAlignment$ Center=�6�   $��0,25,0,0q�6�   
�������6�   �� 5   
   $��Center=�6   $��Center=�62   $��	0,-30,0,0q�6M   $��1��6`   $
��auto��6m   $
��auto��6z    � Cursor$  ��6�   .+�5      ��  �� Orientation$ 
Horizontal=�6   $
��0,10q�62   .+�5      �� $	��160��6   $��Center=�6(   $��Center=�6E   �� 5      $�Фамилия��6   
���   �6-    �� 5      $�*��6   ���	#FF567BFF6'   5      �� 5      -   5        	 ��Name$	 TextBoxFamiliya��5      $	��300��63   $��35��6?   $��10,0,0,0q�6K   $��8,4,0,0q�6]   $��16(�6o   5      �� 5      $ 
Horizontal=�6   $
��0,10q�62   .+�5      �� $	��160��6   $��Center=�6(   $��Center=�6E   �� 5       $�Имя��6   
���   �6)    �� 5!      $�*��6   ���	#FF567BFF6'   5"      �� 5#      -   5        $	 TextBoxImya��5#      $	��300��6/   $��35��6;   $��10,0,0,0q�6G   $��8,4,0,0q�6Y   $��16(�6k   5$      �� 5%      $ 
Horizontal=�6   $
��0,10q�62   .+�5&      �� $	��160��6   $��Center=�6(   $��Center=�6E   �� 5'      $�Отчество��6   
���   �6.   5(      �� 5)      -   5        $	 TextBoxOtchestvo��5)      $	��300��64   $��35��6@   $��10,0,0,0q�6L   $��8,4,0,0q�6^   $��16(�6p   5*      �� 5+      $ 
Horizontal=�6   $
��0,10q�62   .+�5,      �� $	��160��6   $��Center=�6(   $��Center=�6E   �� 5-      $�Дата рождения��6   
���   �63    �� 5.      $�*��6   ���	#FF567BFF6'   5/      �� 50      -   5        
 ��Name$
 BtnData_rojdeniya��50      $0��*Выберите дату рождения��64      GrayComboButtonStyle#��[  6U   $	��300��6�   $��10,0,0,0q�6�   $��33��6�   $��15(�6�   51      �� 52      $ 
Horizontal=�6   $
��0,10q�62   .+�53      �� $	��160��6   $��Center=�6(   $��Center=�6E   �� 54      $!�Номер телефона��6   
���   �64    �� 55      $�*��6   ���	#FF567BFF6'   56      �� 57      -   5        $	 TextBoxNomer_telefona��57      $	��300��69   $��35��6E   $��10,0,0,0q�6Q   $��8,4,0,0q�6c   $��16(�6u   58      �� 5:      -   5        $��BtnSave��5:   ?   $��40��6   $	��250��6!   $��0,30,0,0q�6-   $��Center=�6N   $��Bottom=�6k   $��Сохранить��5;       $��1��64     LoginButtonStyle#��[ 6A   6m   5<      5@      5A      