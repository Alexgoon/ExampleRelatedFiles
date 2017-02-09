for dr in */ ; do
	curl -u Alexgoon:777perec777 https://api.github.com/repos/Alexgoon/${dr%/} -X DELETE
done

find . -name ".git" -exec rm -rf {} \;

echo ready
read