   M S B A M L   `   `   `  ����L  HJenkinsJobCreator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:JenkinsJobCreatorJenkinsJobCreator  5   	   "    JenkinsJobCreator.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35a ]DevExpress.Xpf.Core.v16.2, Version=16.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aF 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   1localclr-namespace:JenkinsJobCreator     5   	   ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5   	   ] YDevExpress.Mvvm.v16.2, Version=16.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5   	   ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5   	   &   DevExpress.Xpf.Core.ThemeManager    	ThemeName0  *DevExpress.Xpf.Core.ThemeNameTypeConverter$  Office2016White 5   	    � Title$ 
MainWindow��5
   	   $	��600��6   $	��810��6)    � DataContext 5   	   1  +DevExpress.Mvvm.UI.ViewModelSourceExtension     Type$ local:MainViewModelL�.��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��2* �6   �� 5      .+�5   
   ��  �� Orientation$ 
Horizontal=�6   $��4q�6.   .+�5      �� $��	Org Name:��6   $��1��6)   �� 5      -   5        $��orgNameTextBox��5   v   $��1��6   $��1��6&   $	��200��63   $��2,0,0,0q�6?   ��6P   �� *	OrgName+ �� Mode$ TwoWay=�6v   �� 5      $��GetList��6   $��2,0,0,0q�6'   ��68   �� *PopulateReposCommand+��6a   ��  �� ElementName$ orgNameTextBox�� �� Path$
 Text��� 5      $��Jenkins Host:��6   $��20,0,0,0q�6-   �� 5      -   5        $��jenkinsHostTextBox��5   `   $��1��6   $	��200��6&   $��2,0,0,0q�62   $��http://localhost:8080��6C   6`   �� 5      $��
CreateJobs��6   $��2,0,0,0q�6�   ��6*   �� *CreateJobsCommand+��6P   �� $ jenkinsHostTextBox��$
 Text�6�   �� 5      $��
RemoveJobs��6   $��2,0,0,0q�6�   ��6*   �� *RemoveJobsCommand+��6P   �� $ jenkinsHostTextBox��$
 Text�6�   5      *  $DevExpress.Xpf.Core.LoadingDecorator  5   
   $��1��6H   	   IsSplashScreenShown	 6   �� *	IsLoading+
   LoadingChild.
 5      ��  �� DisplayMemberPath$
 Name��6   ��6/   �� *Repos+5      �� 5   
   $��3��63   ��6   �� *
ErrorLog+63   5       5"      