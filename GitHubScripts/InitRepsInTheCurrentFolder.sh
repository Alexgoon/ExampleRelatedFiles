userName="Alexgoon"
pass="777perec777"

function BuildLangRepo {
	repoName=$1
	lang=$2
	shouldAddRemoteRepo=$3
	initialDir=$(pwd)
	
	echo $repoName processing
	for rangeDir in $repoName/$lang/* ; do

		cd $initialDir/$rangeDir
		
		git init

		if [ "$shouldAddRemoteRepo" = true ] ; then
		
			echo "github request..."
			curl -u $userName:$pass https://api.github.com/user/repos -d '{"name"':'"'$repoName'"'}
			
			shouldAddRemoteRepo=false
		fi
		
		echo "adding origin... git@github.com:$userName/$repoName.git"
		git remote add origin git@github.com:$userName/$repoName.git
		
		echo "adding files..."
		git add *

		echo "committing..."
		git commit -m 'initial commit'
		
		echo "creating a local branch..."
		git checkout -b ${rangeDir##*/}"_$lang" 
		
		echo "pushing..."
		git push origin ${rangeDir##*/}"_$lang"  

		cd $initialDir
	done
}

SECONDS=0
for dr in */ ; do
	BuildLangRepo ${dr%/} CS true
	BuildLangRepo ${dr%/} VB false
done
echo "$SECONDS seconds elapsed"
echo ready!

read